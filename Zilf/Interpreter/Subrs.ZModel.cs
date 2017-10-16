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
using System.Diagnostics;
using System.Diagnostics.Contracts;
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
using JetBrains.Annotations;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {

        #region Z-Code: Routines, Objects, Constants, Globals

        /// <exception cref="InterpreterError">Unrecognized flag.</exception>
        [NotNull]
        [Subr("ROUTINE-FLAGS")]
        public static ZilObject ROUTINE_FLAGS([NotNull] Context ctx, [ItemNotNull] [NotNull] ZilAtom[] flags)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(flags != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            var newFlags = RoutineFlags.None;

            foreach (var atom in flags)
            {
                switch (atom.StdAtom)
                {
                    case StdAtom.CLEAN_STACK_P:
                        newFlags |= RoutineFlags.CleanStack;
                        break;

                    default:
                        throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, "ROUTINE-FLAGS", "flag", atom);
                }
            }

            ctx.NextRoutineFlags = newFlags;
            return ctx.TRUE;
        }

        /// <exception cref="InterpreterError"><paramref name="name"/> is already defined, or <paramref name="argList"/> defines too many required parameters for the Z-machine version.</exception>
        [NotNull]
        [FSubr]
        public static ZilObject ROUTINE([NotNull] Context ctx, [NotNull] ZilAtom name,
            [CanBeNull] [Optional] ZilAtom activationAtom, [NotNull] ZilList argList,
            [ItemNotNull] [NotNull] [Required] ZilObject[] body)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Requires(argList != null);
            Contract.Requires(body != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            var oldAtom = ctx.ZEnvironment.InternGlobalName(name);
            if (ctx.GetZVal(oldAtom) != null)
            {
                if (ctx.AllowRedefine)
                {
                    ctx.Redefine(oldAtom);
                    ctx.ZEnvironment.InternGlobalName(name);
                }
                else
                    throw new InterpreterError(InterpreterMessages._0_Already_Defined_1, "ROUTINE", oldAtom.ToStringContext(ctx, false));
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

                public ZilObject Decl
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

                [NotNull]
                public ZilAtom GetAtom(Context ctx)
                {
                    if (Content is ZilAtom atom)
                        return atom;

                    return ZilAtom.Parse((string)Content, ctx);
                }

                public override string ToString()
                {
                    return Content.ToString();
                }
            }
        }
#pragma warning restore CS0649

        /// <exception cref="InterpreterError"><paramref name="name"/> is already defined.</exception>
        [FSubr]
        [FSubr("MSETG")]
        public static ZilResult CONSTANT([NotNull] Context ctx,
            AtomParams.AdeclOrAtom name, [NotNull] ZilObject value)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(value != null);
            SubrContracts(ctx);

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
                else if (previous is ZilConstant cnst && cnst.Value.Equals(value))
                {
                    // silently ignore duplicate constants as long as the values are equal
                    return previous;
                }
                else
                {
                    throw new InterpreterError(InterpreterMessages._0_Already_Defined_1, "CONSTANT", oldAtom.ToStringContext(ctx, false));
                }
            }

            var result = ctx.AddZConstant(atom, value);
            result.SourceLine = ctx.TopFrame.SourceLine;
            return result;
        }

        /// <exception cref="InterpreterError"><paramref name="name"/> is already defined.</exception>
        [FSubr]
        public static ZilResult GLOBAL(
            [NotNull] Context ctx,
            AtomParams.AdeclOrAtom name,
            ZilObject defaultValue,
#pragma warning disable RECS0154 // Parameter is never used
            [CanBeNull] ZilObject decl = null,
            [CanBeNull] ZilAtom size = null)
#pragma warning restore RECS0154 // Parameter is never used
        {
            Contract.Requires(ctx != null);
            SubrContracts(ctx);

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
                if (ctx.AllowRedefine)
                {
                    if (oldVal is ZilGlobal glob && glob.Value is ZilTable tbl)
                    {
                        // prevent errors about duplicate symbol T?GLOBAL-NAME
                        // TODO: undefine the table if it hasn't been referenced anywhere yet
                        tbl.Name = null;
                    }

                    ctx.Redefine(oldAtom);
                    ctx.ZEnvironment.InternGlobalName(atom);
                }
                else
                    throw new InterpreterError(InterpreterMessages._0_Already_Defined_1, "GLOBAL", oldAtom.ToStringContext(ctx, false));
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
                public ZilAtom Size;

                [ZilOptional]
                public ZilObject Initializer;
            }
        }
#pragma warning restore CS0649

        [FSubr("DEFINE-GLOBALS")]
        public static ZilResult DEFINE_GLOBALS(
            [NotNull] Context ctx,
#pragma warning disable RECS0154 // Parameter is never used
            ZilAtom groupName,
#pragma warning restore RECS0154 // Parameter is never used
            [NotNull] DefineGlobalsParams.GlobalSpec[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            SubrContracts(ctx);

            foreach (var spec in args)
            {
                var name = spec.Name.Atom;

                // create global and macros
                var globalAtom = ZilAtom.Parse("G?" + name.Text, ctx);
                ZilObject initializer;
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

                Program.Evaluate(ctx, string.Format(SMacroTemplate, name, globalAtom), true);
            }

            // enable FUNNY-GLOBALS?
            ctx.SetGlobalVal(ctx.GetStdAtom(StdAtom.DO_FUNNY_GLOBALS_P), ctx.TRUE);

            return ctx.TRUE;
        }

        [NotNull]
        [Subr]
        public static ZilObject OBJECT([NotNull] Context ctx, [NotNull] ZilAtom name,
            [NotNull] [Decl("<LIST [REST LIST]>")] ZilList[] props)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformObject(ctx, name, props, false);
        }

        [NotNull]
        [Subr]
        public static ZilObject ROOM([NotNull] Context ctx, [NotNull] ZilAtom name,
            [NotNull] [Decl("<LIST [REST LIST]>")] ZilList[] props)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformObject(ctx, name, props, true);
        }

        [NotNull]
        static ZilObject PerformObject([NotNull] Context ctx, [NotNull] ZilAtom atom, [NotNull] ZilList[] props, bool isRoom)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(atom != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            string name = isRoom ? "ROOM" : "OBJECT";

            var oldAtom = ctx.ZEnvironment.InternGlobalName(atom);
            if (ctx.GetZVal(oldAtom) != null)
            {
                if (ctx.AllowRedefine)
                {
                    ctx.Redefine(atom);
                    ctx.ZEnvironment.InternGlobalName(atom);
                }
                else
                    throw new InterpreterError(InterpreterMessages._0_Already_Defined_1, name, oldAtom.ToStringContext(ctx, false));
            }

            var zmo = new ZilModelObject(atom, props, isRoom) { SourceLine = ctx.TopFrame.SourceLine };
            ctx.SetZVal(atom, zmo);
            ctx.ZEnvironment.Objects.Add(zmo);
            return zmo;
        }

        [FSubr]
        public static ZilResult PROPDEF([NotNull] Context ctx, [NotNull] ZilAtom atom, [NotNull] ZilObject defaultValue, ZilObject[] spec)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(atom != null);
            Contract.Requires(defaultValue != null);
            SubrContracts(ctx);

            if (ctx.ZEnvironment.PropertyDefaults.ContainsKey(atom))
                ctx.HandleError(new InterpreterError(InterpreterMessages.Overriding_Default_Value_For_Property_0, atom));

            var zr = defaultValue.Eval(ctx);
            if (zr.ShouldPass())
                return zr;

            ctx.ZEnvironment.PropertyDefaults[atom] = (ZilObject)zr;

            // complex property patterns
            if (spec.Length > 0)
            {
                var pattern = ComplexPropDef.Parse(spec);
                ctx.SetPropDef(atom, pattern);
            }

            return atom;
        }

        [Subr]
        public static ZilObject ZSTART([NotNull] Context ctx, ZilAtom atom)
        {
            Contract.Requires(ctx != null);
            SubrContracts(ctx);

            ctx.ZEnvironment.EntryRoutineName = atom;
            return atom;
        }

        /// <exception cref="InterpreterError">One of the <paramref name="synonyms"/> is already defined.</exception>
        [NotNull]
        [Subr("BIT-SYNONYM")]
        public static ZilObject BIT_SYNONYM([NotNull] Context ctx, [NotNull] ZilAtom first,
            [NotNull] [Required] ZilAtom[] synonyms)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(first != null);
            Contract.Requires(synonyms != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

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
        [NotNull]
        [Subr]
        public static ZilObject ITABLE([NotNull] Context ctx,
            [CanBeNull] [Optional] ZilAtom specifier,
            int count,
            [CanBeNull] [Optional, Decl("<LIST [REST ATOM]>")] ZilList flagList,
            ZilObject[] initializer)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            // Syntax:
            //    <ITABLE [specifier] count [(flags...)] [init...]>
            // 'count' is a number of repetitions.
            // 'specifier' controls the length marker. BYTE specifier
            // makes the length marker a byte (but the table is still a
            // word table unless changed with a flag).
            // 'init' is a sequence of values to be repeated 'count' times.
            // values are compiled as words unless BYTE/LEXV flag is specified.

            TableFlags flags = 0;

            // optional specifier
            if (specifier != null)
            {
                switch (specifier.StdAtom)
                {
                    case StdAtom.NONE:
                        // no change
                        break;
                    case StdAtom.BYTE:
                        flags = TableFlags.ByteLength;
                        break;
                    case StdAtom.WORD:
                        flags = TableFlags.WordLength;
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
                            flags |= TableFlags.Byte;
                            break;
                        case StdAtom.WORD:
                            flags &= ~TableFlags.Byte;
                            break;
                        case StdAtom.LENGTH:
                            gotLength = true;
                            break;
                        case StdAtom.LEXV:
                            flags |= TableFlags.Lexv;
                            break;
                        case StdAtom.PURE:
                            flags |= TableFlags.Pure;
                            break;
                        case StdAtom.PARSER_TABLE:
                            flags |= TableFlags.Pure | TableFlags.ParserTable;
                            break;
                        case StdAtom.TEMP_TABLE:
                            flags |= TableFlags.TempTable;
                            break;
                        default:
                            throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, "ITABLE", "flag", flag);
                    }
                }

                if (gotLength)
                {
                    if ((flags & TableFlags.Byte) != 0)
                        flags |= TableFlags.ByteLength;
                    else
                        flags |= TableFlags.WordLength;
                }
            }

            if (initializer.Length == 0)
                initializer = null;

            var tab = ZilTable.Create(count, initializer, flags, null);
            tab.SourceLine = ctx.TopFrame.SourceLine;
            if ((flags & TableFlags.TempTable) == 0)
                ctx.ZEnvironment.Tables.Add(tab);
            return tab;
        }

        [NotNull]
        static ZilTable PerformTable([NotNull] Context ctx, ZilList flagList, [ItemNotNull] [NotNull] ZilObject[] values,
            bool pure, bool wantLength)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(values != null);
            Contract.Ensures(Contract.Result<ZilTable>() != null);
            SubrContracts(ctx);

            // syntax:
            //    <[P][L]TABLE [(flags...)] values...>

            string name = pure ?
                (wantLength ? "PLTABLE" : "PTABLE") :
                (wantLength ? "LTABLE" : "TABLE");

            const int T_WORDS = 0;
            const int T_BYTES = 1;
            const int T_STRING = 2;
            int type = T_WORDS;
            ZilObject[] pattern = null;
            bool tempTable = false;
            bool parserTable = false;

            if (flagList != null)
            {
                while (!flagList.IsEmpty)
                {
                    Debug.Assert(flagList.Rest != null);

                    if (!(flagList.First is ZilAtom flag))
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
                            if (flagList.IsEmpty || !(flagList.First is ZilList patternList))
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
                            throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, name, "flag", flag);
                    }

                    flagList = flagList.Rest;
                }
            }

            TableFlags flags = 0;
            if (pure)
                flags |= TableFlags.Pure;
            if (type == T_BYTES || type == T_STRING)
                flags |= TableFlags.Byte;
            if (wantLength)
            {
                if (type == T_BYTES || type == T_STRING)
                    flags |= TableFlags.ByteLength;
                else
                    flags |= TableFlags.WordLength;
            }
            if (tempTable)
                flags |= TableFlags.TempTable;
            if (parserTable)
                flags |= TableFlags.ParserTable;

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

            var tab = ZilTable.Create(1, newValues.ToArray(), flags, pattern?.ToArray());
            tab.SourceLine = ctx.TopFrame.SourceLine;
            if (!tempTable)
                ctx.ZEnvironment.Tables.Add(tab);
            return tab;
        }

        static void ValidateTablePattern([NotNull] string name, [NotNull] ZilObject[] pattern)
        {
            Contract.Requires(name != null);
            Contract.Requires(pattern != null);
            Contract.Requires(Contract.ForAll(pattern, p => p != null));

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
                    if (!(vector[0] is ZilAtom atom) || atom.StdAtom != StdAtom.REST)
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

        [NotNull]
        [Subr]
        public static ZilObject TABLE([NotNull] Context ctx, [CanBeNull] [Optional] ZilList flagList,
            [ItemNotNull] [NotNull] ZilObject[] values)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(values != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformTable(ctx, flagList, values, false, false);
        }

        [NotNull]
        [Subr]
        public static ZilObject LTABLE([NotNull] Context ctx, [CanBeNull] [Optional] ZilList flagList,
            [ItemNotNull] [NotNull] ZilObject[] values)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(values != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformTable(ctx, flagList, values, false, true);
        }

        [NotNull]
        [Subr]
        public static ZilObject PTABLE([NotNull] Context ctx, [CanBeNull] [Optional] ZilList flagList,
            [NotNull] ZilObject[] values)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(values != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformTable(ctx, flagList, values, true, false);
        }

        [NotNull]
        [Subr]
        public static ZilObject PLTABLE([NotNull] Context ctx, [CanBeNull] [Optional] ZilList flagList,
            [NotNull] ZilObject[] values)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(values != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformTable(ctx, flagList, values, true, true);
        }

        /// <exception cref="InterpreterError"><paramref name="index"/> is out of range, or the element at <paramref name="index"/> is not a word.</exception>
        [NotNull]
        [Subr]
        public static ZilObject ZGET([NotNull] Context ctx, [NotNull] [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int index)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(tableish != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            if (index < 0)
                throw new InterpreterError(InterpreterMessages._0_Negative_Element_Count, "ZGET");

            var table = (ZilTable)tableish.GetPrimitive(ctx);

            if (index * 2 > table.ByteCount - 2)
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
        [NotNull]
        [Subr]
        public static ZilObject ZPUT([NotNull] Context ctx, [NotNull] [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int index,
            [NotNull] ZilObject newValue)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(tableish != null);
            Contract.Requires(newValue != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            if (index < 0)
                throw new InterpreterError(InterpreterMessages._0_Negative_Element_Count, "ZPUT");

            var table = (ZilTable)tableish.GetPrimitive(ctx);

            if (index * 2 > table.ByteCount - 2)
                throw new InterpreterError(InterpreterMessages._0_Writing_Past_End_Of_Structure, "ZPUT");

            table.PutWord(ctx, index, newValue);
            return newValue;
        }

        [NotNull]
        [Subr]
        public static ZilObject GETB([NotNull] Context ctx, [NotNull] [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int index)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(tableish != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

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

        [NotNull]
        [Subr]
        public static ZilObject PUTB([NotNull] Context ctx, [NotNull] [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int index,
            [NotNull] ZilObject newValue)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(tableish != null);
            Contract.Requires(newValue != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            if (index < 0)
                throw new InterpreterError(InterpreterMessages._0_Negative_Element_Count, "PUTB");

            var table = (ZilTable)tableish.GetPrimitive(ctx);

            if (index >= table.ByteCount)
                throw new InterpreterError(InterpreterMessages._0_Writing_Past_End_Of_Structure, "PUTB");

            table.PutByte(ctx, index, newValue);
            return newValue;
        }

        [Subr]
        public static ZilObject ZREST([NotNull] Context ctx, [NotNull] [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int bytes)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(tableish != null);
            SubrContracts(ctx);

            if (bytes < 0)
                throw new InterpreterError(InterpreterMessages._0_Negative_Element_Count, "ZREST");

            var table = (ZilTable)tableish.GetPrimitive(ctx);

            if (bytes > table.ByteCount)
                throw new InterpreterError(InterpreterMessages._0_Reading_Past_End_Of_Structure, "ZREST");

            return table.OffsetByBytes(bytes);
        }

        #endregion

        #region Z-Code: Version, Options, Capabilities

        [NotNull]
        [Subr]
        public static ZilObject VERSION([NotNull] Context ctx,
            [NotNull] ZilObject versionExpr,
            [CanBeNull] [Decl("'TIME")] ZilAtom time = null)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(versionExpr != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            var newVersion = ParseZVersion("VERSION", versionExpr);

            ctx.SetZVersion(newVersion);

            if (time != null)
            {
                if (ctx.ZEnvironment.ZVersion != 3)
                    throw new InterpreterError(InterpreterMessages._0_TIME_Is_Only_Meaningful_In_Version_3, "VERSION");

                ctx.ZEnvironment.TimeStatusLine = true;
            }

            return new ZilFix(newVersion);
        }

        static int ParseZVersion([NotNull] string name, [NotNull] ZilObject expr)
        {
            Contract.Requires(name != null);
            Contract.Requires(expr != null);
            Contract.Ensures(Contract.Result<int>() >= 1 && Contract.Result<int>() <= 8);

            int newVersion;
            switch (expr)
            {
                case ZilAtom atom:
                    string text = atom.Text;
                    goto HandleNamedVersion;

                case ZilString str:
                    text = str.Text;

                HandleNamedVersion:
                    switch (text.ToUpperInvariant())
                    {
                        case "ZIP":
                            newVersion = 3;
                            break;
                        case "EZIP":
                            newVersion = 4;
                            break;
                        case "XZIP":
                            newVersion = 5;
                            break;
                        case "YZIP":
                            newVersion = 6;
                            break;
                        default:
                            throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, name, "version name", text)
                                .Combine(new InterpreterError(InterpreterMessages.Recognized_Versions_Are_ZIP_EZIP_XZIP_YZIP_And_Numbers_38));
                    }
                    break;

                case ZilFix fix:
                    newVersion = fix.Value;
                    if (newVersion < 3 || newVersion > 8)
                        throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, name, "version number", newVersion)
                            .Combine(new InterpreterError(InterpreterMessages.Recognized_Versions_Are_ZIP_EZIP_XZIP_YZIP_And_Numbers_38));
                    break;

                default:
                    throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, name, "version specifier", expr)
                        .Combine(new InterpreterError(InterpreterMessages.Recognized_Versions_Are_ZIP_EZIP_XZIP_YZIP_And_Numbers_38));
            }
            return newVersion;
        }

        [NotNull]
        [Subr("CHECK-VERSION?")]
        public static ZilObject CHECK_VERSION_P([NotNull] Context ctx, [NotNull] ZilObject versionExpr)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(versionExpr != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            var version = ParseZVersion("CHECK-VERSION?", versionExpr);
            return ctx.ZEnvironment.ZVersion == version ? ctx.TRUE : ctx.FALSE;
        }

        [FSubr("VERSION?")]
        public static ZilResult VERSION_P([NotNull] Context ctx,
            [NotNull] CondClause[] clauses)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(clauses != null);
            SubrContracts(ctx);

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

        [NotNull]
        [Subr("ORDER-OBJECTS?")]
        public static ZilObject ORDER_OBJECTS_P([NotNull] Context ctx, [NotNull] ZilAtom atom)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(atom != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

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

        [NotNull]
        [Subr("ORDER-TREE?")]
        public static ZilObject ORDER_TREE_P([NotNull] Context ctx, [NotNull] ZilAtom atom)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(atom != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

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
            [NotNull] [Required] ZilAtom[] objects)
        {
            Contract.Requires(objects != null);
            SubrContracts(ctx);

            foreach (var atom in objects)
            {
                ctx.ZEnvironment.FlagsOrderedLast.Add(atom);
            }

            return order;
        }

        /// <exception cref="InterpreterError">Unrecognized option.</exception>
        [NotNull]
        [Subr("ZIP-OPTIONS")]
        public static ZilObject ZIP_OPTIONS([NotNull] Context ctx, [NotNull] ZilAtom[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

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
                        throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, "ZIP-OPTIONS", "option", atom);
                }

                ctx.DefineCompilationFlag(atom, ctx.TRUE, true);
                ctx.SetGlobalVal(ctx.GetStdAtom(flag), ctx.TRUE);
            }

            return ctx.TRUE;
        }

        [NotNull]
        [Subr("LONG-WORDS?")]
        public static ZilObject LONG_WORDS_P([NotNull] Context ctx, bool enabled = true)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            ctx.DefineCompilationFlag(ctx.GetStdAtom(StdAtom.LONG_WORDS),
                enabled ? ctx.TRUE : ctx.FALSE, true);
            return ctx.TRUE;
        }

        [NotNull]
        [Subr("FUNNY-GLOBALS?")]
        public static ZilObject FUNNY_GLOBALS_P([NotNull] Context ctx, bool enabled = true)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            ctx.SetGlobalVal(ctx.GetStdAtom(StdAtom.DO_FUNNY_GLOBALS_P),
                enabled ? ctx.TRUE : ctx.FALSE);
            return ctx.TRUE;
        }

        [NotNull]
        [Subr]
        public static ZilObject CHRSET(Context ctx, int alphabetNum,
            [NotNull] [Required, Decl("<LIST [REST <OR STRING CHARACTER FIX BYTE>]>")] ZilObject[] args)
        {
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

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

        [NotNull]
        [Subr]
        public static ZilObject LANGUAGE([NotNull] Context ctx, [NotNull] ZilAtom name, char escapeChar = '%', bool changeChrset = true)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(name != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);
            var language = ZModel.Language.Get(name.Text) ??
                throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, "LANGUAGE", "language", name.Text);

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

        [NotNull]
        [Subr]
        public static ZilObject DIRECTIONS([NotNull] Context ctx, [NotNull] [Required] ZilAtom[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            // if a PROPSPEC is set for DIRECTIONS, it'll be copied to the new direction properties
            var propspecAtom = ctx.GetStdAtom(StdAtom.PROPSPEC);
            var propspec = ctx.GetProp(ctx.GetStdAtom(StdAtom.DIRECTIONS), propspecAtom);

            ctx.ZEnvironment.Directions.Clear();
            foreach (ZilAtom arg in args)
            {
                ctx.ZEnvironment.Directions.Add(arg);
                ctx.ZEnvironment.GetVocabDirection(arg, ctx.TopFrame.SourceLine);
                ctx.ZEnvironment.LowDirection = arg;

                ctx.PutProp(arg, propspecAtom, propspec);
            }

            return ctx.TRUE;
        }

        [NotNull]
        [Subr]
        public static ZilObject BUZZ([NotNull] Context ctx, [NotNull] [Required] ZilAtom[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            foreach (ZilAtom arg in args)
                ctx.ZEnvironment.Buzzwords.Add(new KeyValuePair<ZilAtom, ISourceLine>(arg, ctx.TopFrame.SourceLine));

            return ctx.TRUE;
        }

        [NotNull]
        [Subr]
        public static ZilObject VOC([NotNull] Context ctx, [NotNull] string text, [CanBeNull] [Decl("<OR FALSE ATOM>")] ZilObject type = null)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(text != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

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
                        throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, "VOC", "part of speech", type);
                }
            }

            return ctx.ChangeType(atom, ctx.GetStdAtom(StdAtom.VOC));
        }

        /// <exception cref="ArgumentCountError"><paramref name="args"/> is too short.</exception>
        [NotNull]
        [Subr]
        public static ZilObject SYNTAX([NotNull] Context ctx, [NotNull] ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(args != null);
            SubrContracts(ctx, args);

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

        [NotNull]
        static ZilObject PerformSynonym([NotNull] Context ctx, [NotNull] ZilAtom original, [NotNull] ZilAtom[] synonyms, [NotNull] Type synonymType)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);
            Contract.Requires(original != null);
            Contract.Requires(synonyms != null);
            Contract.Requires(Contract.ForAll(synonyms, s => s != null));
            Contract.Requires(synonymType != null);

            if (ctx.ZEnvironment.Vocabulary.TryGetValue(original, out var oldWord) == false)
            {
                oldWord = ctx.ZEnvironment.VocabFormat.CreateWord(original);
                ctx.ZEnvironment.Vocabulary.Add(original, oldWord);
            }

            object[] ctorArgs = new object[2];
            ctorArgs[0] = oldWord;

            foreach (var synonym in synonyms)
            {
                if (ctx.ZEnvironment.Vocabulary.TryGetValue(synonym, out var newWord) == false)
                {
                    newWord = ctx.ZEnvironment.VocabFormat.CreateWord(synonym);
                    ctx.ZEnvironment.Vocabulary.Add(synonym, newWord);
                }

                ctorArgs[1] = newWord;
                ctx.ZEnvironment.Synonyms.Add((Synonym)Activator.CreateInstance(
                    synonymType, ctorArgs));
            }

            return original;
        }

        [NotNull]
        [Subr]
        public static ZilObject SYNONYM([NotNull] Context ctx, [NotNull] ZilAtom original, [NotNull] ZilAtom[] synonyms)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(original != null);
            Contract.Requires(synonyms != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformSynonym(ctx, original, synonyms, typeof(Synonym));
        }

        [NotNull]
        [Subr("VERB-SYNONYM")]
        public static ZilObject VERB_SYNONYM([NotNull] Context ctx, [NotNull] ZilAtom original, [NotNull] ZilAtom[] synonyms)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(original != null);
            Contract.Requires(synonyms != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformSynonym(ctx, original, synonyms, typeof(VerbSynonym));
        }

        [NotNull]
        [Subr("PREP-SYNONYM")]
        public static ZilObject PREP_SYNONYM([NotNull] Context ctx, [NotNull] ZilAtom original, [NotNull] ZilAtom[] synonyms)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(original != null);
            Contract.Requires(synonyms != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformSynonym(ctx, original, synonyms, typeof(PrepSynonym));
        }

        [NotNull]
        [Subr("ADJ-SYNONYM")]
        public static ZilObject ADJ_SYNONYM([NotNull] Context ctx, [NotNull] ZilAtom original, [NotNull] ZilAtom[] synonyms)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(original != null);
            Contract.Requires(synonyms != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformSynonym(ctx, original, synonyms, typeof(AdjSynonym));
        }

        [NotNull]
        [Subr("DIR-SYNONYM")]
        public static ZilObject DIR_SYNONYM([NotNull] Context ctx, [NotNull] ZilAtom original, [NotNull] ZilAtom[] synonyms)
        {
            Contract.Requires(ctx != null);
            Contract.Requires(original != null);
            Contract.Requires(synonyms != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx);

            return PerformSynonym(ctx, original, synonyms, typeof(DirSynonym));
        }

        #endregion

        #region Z-Code: Tell

        [NotNull]
        [FSubr("TELL-TOKENS")]
        public static ZilObject TELL_TOKENS([NotNull] Context ctx, [NotNull] ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx, args);

            ctx.ZEnvironment.TellPatterns.Clear();
            return ADD_TELL_TOKENS(ctx, args);
        }

        [NotNull]
        [FSubr("ADD-TELL-TOKENS")]
        public static ZilObject ADD_TELL_TOKENS([NotNull] Context ctx, [NotNull] ZilObject[] args)
        {
            Contract.Requires(ctx != null);
            Contract.Ensures(Contract.Result<ZilObject>() != null);
            SubrContracts(ctx, args);

            ctx.ZEnvironment.TellPatterns.AddRange(TellPattern.Parse(args));
            return ctx.TRUE;
        }

        #endregion

        #region Z-Code: Version 6 Parser

        /// <exception cref="InterpreterError">NEW-PARSER? is not enabled.</exception>
        [NotNull]
        [Subr("ADD-WORD")]
        [Subr("NEW-ADD-WORD")]
        public static ZilObject NEW_ADD_WORD([NotNull] Context ctx,
            AtomParams.StringOrAtom name,
            [CanBeNull] ZilAtom type = null,
            [CanBeNull] ZilObject value = null,
            ZilFix flags = null)
        {
            Contract.Requires(ctx != null);
            SubrContracts(ctx);

            if (!ctx.GetGlobalOption(StdAtom.NEW_PARSER_P))
                throw new InterpreterError(InterpreterMessages._0_Requires_NEWPARSER_Option, "NEW-ADD-WORD");

            var nameAtom = name.GetAtom(ctx);
            flags = flags ?? ZilFix.Zero;
            return ((NewParserVocabFormat)ctx.ZEnvironment.VocabFormat).NewAddWord(nameAtom, type, value, flags);
        }

        #endregion
    }
}
