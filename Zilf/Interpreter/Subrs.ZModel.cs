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
using System.Runtime.InteropServices;
using System.Text;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Values;
using Zilf.ZModel.Vocab;
using Zilf.ZModel.Vocab.NewParser;
using Zilf.Diagnostics;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {

        #region Z-Code: Routines, Objects, Constants, Globals

        [Subr("ROUTINE-FLAGS")]
        public static ZilObject ROUTINE_FLAGS(Context ctx, ZilAtom[] flags)
        {
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

        [FSubr]
        public static ZilObject ROUTINE(Context ctx, ZilAtom name,
            [Optional] ZilAtom activationAtom, ZilList argList,
            [Required] ZilObject[] body)
        {
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
                ctx.HandleWarning(new InterpreterError(ctx.TopFrame.SourceLine,
                    InterpreterMessages._0_Only_1_Routine_Argument1s_Allowed_In_V2_So_Last_3_OPT_Argument3s_Will_Never_Be_Passed,
                    "ROUTINE",
                    maxArgsAllowed,
                    ctx.ZEnvironment.ZVersion,
                    affectedArgCount));
            }

            rtn.SourceLine = ctx.TopFrame.SourceLine;
            ctx.SetZVal(name, rtn);
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

        public static class AtomParams
        {
            [ZilSequenceParam]
            public struct AdeclOrAtom
            {
                [Decl("<OR ATOM <ADECL ATOM>>"), Either(typeof(ZilAtom), typeof(ZilAdecl))]
                public ZilObject Content;

                public ZilAtom Atom
                {
                    get
                    {
                        var atom = Content as ZilAtom;
                        if (atom != null)
                            return atom;

                        return (ZilAtom)((ZilAdecl)Content).First;
                    }
                }

                public ZilObject Decl
                {
                    get
                    {
                        var adecl = Content as ZilAdecl;
                        return adecl?.Second;
                    }
                }
            }

            [ZilSequenceParam]
            public struct StringOrAtom
            {
                [Either(typeof(ZilAtom), typeof(string))]
                public object Content;

                public ZilAtom GetAtom(Context ctx)
                {
                    var atom = Content as ZilAtom;
                    if (atom != null)
                        return atom;

                    return ZilAtom.Parse((string)Content, ctx);
                }

                public override string ToString()
                {
                    return Content.ToString();
                }
            }
        }

        [FSubr]
        [FSubr("MSETG")]
        public static ZilObject CONSTANT(Context ctx,
            AtomParams.AdeclOrAtom name, ZilObject value)
        {
            SubrContracts(ctx);

            var atom = name.Atom;
            value = value.Eval(ctx);

            var oldAtom = ctx.ZEnvironment.InternGlobalName(atom);
            var previous = ctx.GetZVal(oldAtom);
            if (previous != null)
            {
                if (ctx.AllowRedefine)
                {
                    ctx.Redefine(oldAtom);
                    ctx.ZEnvironment.InternGlobalName(atom);
                }
                else if (previous is ZilConstant && ((ZilConstant)previous).Value.Equals(value))
                {
                    // silently ignore duplicate constants as long as the values are equal
                    return previous;
                }
                else
                {
                    throw new InterpreterError(InterpreterMessages._0_Already_Defined_1, "CONSTANT", oldAtom.ToStringContext(ctx, false));
                }
            }

            return ctx.AddZConstant(atom, value);
        }

        [FSubr]
        public static ZilObject GLOBAL(
            Context ctx,
            AtomParams.AdeclOrAtom name,
            ZilObject defaultValue,
#pragma warning disable RECS0154 // Parameter is never used
            ZilObject decl = null,
            ZilAtom size = null)
#pragma warning restore RECS0154 // Parameter is never used
        {
            SubrContracts(ctx);

            // typical form:  <GLOBAL atom-or-adecl default-value>
            // quirky form:   <GLOBAL atom-or-adecl default-value decl [size]>
            // TODO: use decl and size?

            var atom = name.Atom;
            defaultValue = defaultValue.Eval(ctx);

            var oldAtom = ctx.ZEnvironment.InternGlobalName(atom);
            var oldVal = ctx.GetZVal(oldAtom);
            if (oldVal != null)
            {
                if (ctx.AllowRedefine)
                {
                    if (oldVal is ZilGlobal)
                    {
                        var oldDefault = ((ZilGlobal)oldVal).Value;
                        if (oldDefault is ZilTable)
                        {
                            // prevent errors about duplicate symbol T?GLOBAL-NAME
                            // TODO: undefine the table if it hasn't been referenced anywhere yet
                            ((ZilTable)oldDefault).Name = null;
                        }
                    }

                    ctx.Redefine(oldAtom);
                    ctx.ZEnvironment.InternGlobalName(atom);
                }
                else
                    throw new InterpreterError(InterpreterMessages._0_Already_Defined_1, "GLOBAL", oldAtom.ToStringContext(ctx, false));
            }

            if (defaultValue is ZilTable)
                ((ZilTable)defaultValue).Name = "T?" + atom.Text;

            var g = new ZilGlobal(atom, defaultValue);
            ctx.SetZVal(atom, g);
            ctx.ZEnvironment.Globals.Add(g);
            return g;
        }

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

        [FSubr("DEFINE-GLOBALS")]
        public static ZilObject DEFINE_GLOBALS(
            Context ctx,
#pragma warning disable RECS0154 // Parameter is never used
            ZilAtom groupName,
#pragma warning restore RECS0154 // Parameter is never used
            DefineGlobalsParams.GlobalSpec[] args)
        {
            SubrContracts(ctx);

            foreach (var spec in args)
            {
                var name = spec.Name.Atom;

                // create global and macros
                var globalAtom = ZilAtom.Parse("G?" + name, ctx);
                var g = new ZilGlobal(globalAtom, spec.Initializer?.Eval(ctx) ?? ctx.FALSE);
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

        [Subr]
        public static ZilObject OBJECT(Context ctx, ZilAtom name,
            [Decl("<LIST [REST LIST]>")] ZilList[] props)
        {
            SubrContracts(ctx);

            return PerformObject(ctx, name, props, false);
        }

        [Subr]
        public static ZilObject ROOM(Context ctx, ZilAtom name,
            [Decl("<LIST [REST LIST]>")] ZilList[] props)
        {
            SubrContracts(ctx);

            return PerformObject(ctx, name, props, true);
        }

        static ZilObject PerformObject(Context ctx, ZilAtom atom, ZilList[] props, bool isRoom)
        {
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

            var zmo = new ZilModelObject(atom, props, isRoom);
            zmo.SourceLine = ctx.TopFrame.SourceLine;
            ctx.SetZVal(atom, zmo);
            ctx.ZEnvironment.Objects.Add(zmo);
            return zmo;
        }

        [FSubr]
        public static ZilObject PROPDEF(Context ctx, ZilAtom atom, ZilObject defaultValue, ZilObject[] spec)
        {
            SubrContracts(ctx);

            if (ctx.ZEnvironment.PropertyDefaults.ContainsKey(atom))
                ctx.HandleWarning(new InterpreterError(InterpreterMessages.Overriding_Default_Value_For_Property_0, atom));

            ctx.ZEnvironment.PropertyDefaults[atom] = defaultValue.Eval(ctx);

            // complex property patterns
            if (spec.Length > 0)
            {
                var pattern = ComplexPropDef.Parse(spec, ctx);
                ctx.SetPropDef(atom, pattern);
            }

            return atom;
        }

        [Subr]
        public static ZilObject ZSTART(Context ctx, ZilAtom atom)
        {
            SubrContracts(ctx);

            ctx.ZEnvironment.EntryRoutineName = atom;
            return atom;
        }

        [Subr("BIT-SYNONYM")]
        public static ZilObject BIT_SYNONYM(Context ctx, ZilAtom first,
            [Required] ZilAtom[] synonyms)
        {
            SubrContracts(ctx);

            ZilAtom original;
            if (ctx.ZEnvironment.TryGetBitSynonym(first, out original))
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

        [Subr]
        public static ZilObject ITABLE(Context ctx,
            [Optional] ZilAtom specifier,
            int count,
            [Optional, Decl("<LIST [REST ATOM]>")] ZilList flagList,
            ZilObject[] initializer)
        {
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

                foreach (ZilObject obj in flagList)
                {
                    var flag = (ZilAtom)obj;

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

        static ZilTable PerformTable(Context ctx, ZilList flagList, ZilObject[] values,
            bool pure, bool wantLength)
        {
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
                    var flag = flagList.First as ZilAtom;
                    if (flag == null)
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
                            ZilList patternList;
                            if (flagList.IsEmpty || (patternList = flagList.First as ZilList) == null)
                                throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, name, "a list", "PATTERN");
                            pattern = patternList.ToArray();
                            ValidateTablePattern(name, pattern);
                            break;

                        case StdAtom.SEGMENT:
                            // ignore
                            flagList = flagList.Rest;
                            if (flagList.IsEmpty)
                                throw new InterpreterError(InterpreterMessages._0_Expected_1_After_2, name, "a value", "SEGMENT");
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

            var tab = ZilTable.Create(
                1, newValues.ToArray(), flags, pattern == null ? null : pattern.ToArray());
            tab.SourceLine = ctx.TopFrame.SourceLine;
            if (!tempTable)
                ctx.ZEnvironment.Tables.Add(tab);
            return tab;
        }

        static void ValidateTablePattern(string name, ZilObject[] pattern)
        {
            Contract.Requires(name != null);
            Contract.Requires(pattern != null && Contract.ForAll(pattern, p => p != null));

            if (pattern.Length == 0)
                throw new InterpreterError(InterpreterMessages._0_PATTERN_Must_Not_Be_Empty, name);

            for (int i = 0; i < pattern.Length; i++)
            {
                if (IsByteOrWordAtom(pattern[i]))
                {
                    // OK
                    continue;
                }

                var vector = pattern[i] as ZilVector;
                if (vector != null)
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
                    var atom = vector[0] as ZilAtom;
                    if (atom == null || atom.StdAtom != StdAtom.REST)
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
            var atom = value as ZilAtom;
            return atom != null && (atom.StdAtom == StdAtom.BYTE || atom.StdAtom == StdAtom.WORD);
        }

        [Subr]
        public static ZilObject TABLE(Context ctx, [Optional] ZilList flagList, ZilObject[] values)
        {
            SubrContracts(ctx);

            return PerformTable(ctx, flagList, values, false, false);
        }

        [Subr]
        public static ZilObject LTABLE(Context ctx, [Optional] ZilList flagList, ZilObject[] values)
        {
            SubrContracts(ctx);

            return PerformTable(ctx, flagList, values, false, true);
        }

        [Subr]
        public static ZilObject PTABLE(Context ctx, [Optional] ZilList flagList, ZilObject[] values)
        {
            SubrContracts(ctx);

            return PerformTable(ctx, flagList, values, true, false);
        }

        [Subr]
        public static ZilObject PLTABLE(Context ctx, [Optional] ZilList flagList, ZilObject[] values)
        {
            SubrContracts(ctx);

            return PerformTable(ctx, flagList, values, true, true);
        }

        [Subr]
        public static ZilObject ZGET(Context ctx, [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int index)
        {
            SubrContracts(ctx);

            var table = (ZilTable)tableish.GetPrimitive(ctx);

            return table.GetWord(ctx, index) ?? ctx.FALSE;
        }

        [Subr]
        public static ZilObject ZPUT(Context ctx, [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int index, ZilObject newValue)
        {
            SubrContracts(ctx);

            var table = (ZilTable)tableish.GetPrimitive(ctx);

            table.PutWord(ctx, index, newValue);
            return newValue;
        }

        [Subr]
        public static ZilObject GETB(Context ctx, [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int index)
        {
            SubrContracts(ctx);

            var table = (ZilTable)tableish.GetPrimitive(ctx);

            return table.GetByte(ctx, index) ?? ctx.FALSE;
        }

        [Subr]
        public static ZilObject PUTB(Context ctx, [Decl("<PRIMTYPE TABLE>")] ZilObject tableish, int index, ZilObject newValue)
        {
            SubrContracts(ctx);

            var table = (ZilTable)tableish.GetPrimitive(ctx);

            table.PutByte(ctx, index, newValue);
            return newValue;
        }

        [Subr]
        public static ZilObject ZREST(Context ctx, ZilTable table, int bytes)
        {
            SubrContracts(ctx);

            if (bytes < 0)
                throw new InterpreterError(
                    InterpreterMessages._0_Expected_1,
                    "ZREST: arg 2",
                    "non-negative FIX");

            return table.OffsetByBytes(ctx, bytes);
        }

        #endregion

        #region Z-Code: Version, Options, Capabilities

        [Subr]
        public static ZilObject VERSION(Context ctx,
            ZilObject versionExpr,
            [Decl("'TIME")] ZilAtom time = null)
        {
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

        static int ParseZVersion(string name, ZilObject expr)
        {
            Contract.Requires(name != null);
            Contract.Requires(expr != null);
            Contract.Ensures(Contract.Result<int>() >= 1 && Contract.Result<int>() <= 8);

            int newVersion;
            if (expr is ZilAtom || expr is ZilString)
            {
                string text;
                if (expr is ZilAtom)
                    text = ((ZilAtom)expr).Text;
                else
                    text = ((ZilString)expr).Text;

                switch (text.ToUpper())
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
            }
            else if (expr is ZilFix)
            {
                newVersion = ((ZilFix)expr).Value;
                if (newVersion < 3 || newVersion > 8)
                    throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, name, "version number", newVersion)
                        .Combine(new InterpreterError(InterpreterMessages.Recognized_Versions_Are_ZIP_EZIP_XZIP_YZIP_And_Numbers_38));
            }
            else
            {
                throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, name, "version specifier", expr)
                    .Combine(new InterpreterError(InterpreterMessages.Recognized_Versions_Are_ZIP_EZIP_XZIP_YZIP_And_Numbers_38));
            }
            return newVersion;
        }

        [Subr("CHECK-VERSION?")]
        public static ZilObject CHECK_VERSION_P(Context ctx, ZilObject versionExpr)
        {
            SubrContracts(ctx);

            var version = ParseZVersion("CHECK-VERSION?", versionExpr);
            return ctx.ZEnvironment.ZVersion == version ? ctx.TRUE : ctx.FALSE;
        }

        [FSubr("VERSION?")]
        public static ZilObject VERSION_P(Context ctx, 
            CondClause[] clauses)
        {
            SubrContracts(ctx);

            var tAtom = ctx.GetStdAtom(StdAtom.T);
            var elseAtom = ctx.GetStdAtom(StdAtom.ELSE);

            foreach (var clause in clauses)
            {
                if (clause.Condition == tAtom || clause.Condition == elseAtom ||
                    ParseZVersion("VERSION?", clause.Condition) == ctx.ZEnvironment.ZVersion)
                {
                    var result = clause.Condition;
                    foreach (var expr in clause.Body)
                        result = expr.Eval(ctx);
                    return result;
                }
            }

            return ctx.FALSE;
        }

        [Subr("ORDER-OBJECTS?")]
        public static ZilObject ORDER_OBJECTS_P(Context ctx, ZilAtom atom)
        {
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

        [Subr("ORDER-TREE?")]
        public static ZilObject ORDER_TREE_P(Context ctx, ZilAtom atom)
        {
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
            [Required] ZilAtom[] objects)
        {
            SubrContracts(ctx);

            foreach (var atom in objects)
            {
                ctx.ZEnvironment.FlagsOrderedLast.Add(atom);
            }

            return order;
        }

        [Subr("ZIP-OPTIONS")]
        public static ZilObject ZIP_OPTIONS(Context ctx, ZilAtom[] args)
        {
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

        [Subr("LONG-WORDS?")]
        public static ZilObject LONG_WORDS_P(Context ctx, bool enabled = true)
        {
            SubrContracts(ctx);

            ctx.DefineCompilationFlag(ctx.GetStdAtom(StdAtom.LONG_WORDS),
                enabled ? ctx.TRUE : ctx.FALSE, true);
            return ctx.TRUE;
        }

        [Subr("FUNNY-GLOBALS?")]
        public static ZilObject FUNNY_GLOBALS_P(Context ctx, bool enabled = true)
        {
            SubrContracts(ctx);

            ctx.SetGlobalVal(ctx.GetStdAtom(StdAtom.DO_FUNNY_GLOBALS_P),
                enabled ? ctx.TRUE : ctx.FALSE);
            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject CHRSET(Context ctx, int alphabetNum,
            [Required, Decl("<LIST [REST <OR STRING CHARACTER FIX BYTE>]>")] ZilObject[] args)
        {
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
                        // shouldn't get here
                        Contract.Assert(false);
                        throw new NotImplementedException();
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
            SubrContracts(ctx);

            var language = ZModel.Language.Get(name.Text);
            if (language == null)
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

        [Subr]
        public static ZilObject DIRECTIONS(Context ctx, [Required] ZilAtom[] args)
        {
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

        [Subr]
        public static ZilObject BUZZ(Context ctx, [Required] ZilAtom[] args)
        {
            SubrContracts(ctx);

            foreach (ZilAtom arg in args)
                ctx.ZEnvironment.Buzzwords.Add(new KeyValuePair<ZilAtom, ISourceLine>(arg, ctx.TopFrame.SourceLine));

            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject VOC(Context ctx, string text, [Decl("<OR FALSE ATOM>")] ZilObject type = null)
        {
            SubrContracts(ctx);

            var atom = ZilAtom.Parse(text, ctx);
            var word = ctx.ZEnvironment.GetVocab(atom);

            var typeAtom = type as ZilAtom;
            if (typeAtom != null)
            {
                switch (typeAtom.StdAtom)
                {
                    case StdAtom.ADJ:
                    case StdAtom.ADJECTIVE:
                        word = ctx.ZEnvironment.GetVocabAdjective(atom, ctx.TopFrame.SourceLine);
                        break;

                    case StdAtom.NOUN:
                    case StdAtom.OBJECT:
                        word = ctx.ZEnvironment.GetVocabNoun(atom, ctx.TopFrame.SourceLine);
                        break;

                    case StdAtom.BUZZ:
                        word = ctx.ZEnvironment.GetVocabBuzzword(atom, ctx.TopFrame.SourceLine);
                        break;

                    case StdAtom.PREP:
                        word = ctx.ZEnvironment.GetVocabPreposition(atom, ctx.TopFrame.SourceLine);
                        break;

                    case StdAtom.DIR:
                        word = ctx.ZEnvironment.GetVocabDirection(atom, ctx.TopFrame.SourceLine);
                        break;

                    case StdAtom.VERB:
                        word = ctx.ZEnvironment.GetVocabVerb(atom, ctx.TopFrame.SourceLine);
                        break;

                    default:
                        throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, "VOC", "part of speech", type);
                }
            }

            return ctx.ChangeType(atom, ctx.GetStdAtom(StdAtom.VOC));
        }

        [Subr]
        public static ZilObject SYNTAX(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 3)
                throw ArgumentCountError.WrongCount(new FunctionCallSite("SYNTAX"), 3, null);

            var syntax = Syntax.Parse(ctx.TopFrame.SourceLine, args, ctx);
            ctx.ZEnvironment.Syntaxes.Add(syntax);

            if (syntax.Synonyms.Count > 0)
            {
                PerformSynonym(ctx, syntax.Verb.Atom, syntax.Synonyms.ToArray(), "SYNTAX (verb synonyms)", typeof(VerbSynonym));
            }

            return syntax.Verb.Atom;
        }

        static ZilObject PerformSynonym(Context ctx, ZilAtom original, ZilAtom[] synonyms,
            string name, Type synonymType)
        {
            SubrContracts(ctx);
            Contract.Requires(original != null);
            Contract.Requires(synonyms != null);
            Contract.Requires(Contract.ForAll(synonyms, s => s != null));
            Contract.Requires(name != null);
            Contract.Requires(synonymType != null);

            IWord oldWord;
            if (ctx.ZEnvironment.Vocabulary.TryGetValue(original, out oldWord) == false)
            {
                oldWord = ctx.ZEnvironment.VocabFormat.CreateWord(original);
                ctx.ZEnvironment.Vocabulary.Add(original, oldWord);
            }

            object[] ctorArgs = new object[2];
            ctorArgs[0] = oldWord;

            foreach (var synonym in synonyms)
            {
                IWord newWord;
                if (ctx.ZEnvironment.Vocabulary.TryGetValue(synonym, out newWord) == false)
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

        [Subr]
        public static ZilObject SYNONYM(Context ctx, ZilAtom original, ZilAtom[] synonyms)
        {
            SubrContracts(ctx);

            return PerformSynonym(ctx, original, synonyms, "SYNONYM", typeof(Synonym));
        }

        [Subr("VERB-SYNONYM")]
        public static ZilObject VERB_SYNONYM(Context ctx, ZilAtom original, ZilAtom[] synonyms)
        {
            SubrContracts(ctx);

            return PerformSynonym(ctx, original, synonyms, "VERB-SYNONYM", typeof(VerbSynonym));
        }

        [Subr("PREP-SYNONYM")]
        public static ZilObject PREP_SYNONYM(Context ctx, ZilAtom original, ZilAtom[] synonyms)
        {
            SubrContracts(ctx);

            return PerformSynonym(ctx, original, synonyms, "PREP-SYNONYM", typeof(PrepSynonym));
        }

        [Subr("ADJ-SYNONYM")]
        public static ZilObject ADJ_SYNONYM(Context ctx, ZilAtom original, ZilAtom[] synonyms)
        {
            SubrContracts(ctx);

            return PerformSynonym(ctx, original, synonyms, "ADJ-SYNONYM", typeof(AdjSynonym));
        }

        [Subr("DIR-SYNONYM")]
        public static ZilObject DIR_SYNONYM(Context ctx, ZilAtom original, ZilAtom[] synonyms)
        {
            SubrContracts(ctx);

            return PerformSynonym(ctx, original, synonyms, "DIR-SYNONYM", typeof(DirSynonym));
        }

        #endregion

        #region Z-Code: Tell

        [FSubr("TELL-TOKENS")]
        public static ZilObject TELL_TOKENS(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            ctx.ZEnvironment.TellPatterns.Clear();
            return ADD_TELL_TOKENS(ctx, args);
        }

        [FSubr("ADD-TELL-TOKENS")]
        public static ZilObject ADD_TELL_TOKENS(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            ctx.ZEnvironment.TellPatterns.AddRange(TellPattern.Parse(args, ctx));
            return ctx.TRUE;
        }

        #endregion

        #region Z-Code: Version 6 Parser

        [Subr("ADD-WORD")]
        [Subr("NEW-ADD-WORD")]
        public static ZilObject NEW_ADD_WORD(Context ctx,
            AtomParams.StringOrAtom name,
            ZilAtom type = null,
            ZilObject value = null,
            ZilFix flags = null)
        {
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
