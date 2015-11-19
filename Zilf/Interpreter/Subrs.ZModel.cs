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
using System.Text;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel;
using Zilf.ZModel.Values;
using Zilf.ZModel.Vocab;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {

        #region Z-Code: Routines, Objects, Constants, Globals

        [Subr("ROUTINE-FLAGS")]
        public static ZilObject ROUTINE_FLAGS(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            var newFlags = RoutineFlags.None;

            foreach (var arg in args)
            {
                var atom = arg as ZilAtom;
                if (atom == null)
                    throw new InterpreterError("ROUTINE-FLAGS: all args must be atoms");

                switch (atom.StdAtom)
                {
                    case StdAtom.CLEAN_STACK_P:
                        newFlags |= RoutineFlags.CleanStack;
                        break;

                    default:
                        throw new InterpreterError("ROUTINE-FLAGS: unrecognized flag: " + atom);
                }
            }

            ctx.NextRoutineFlags = newFlags;
            return ctx.TRUE;
        }

        [FSubr]
        public static ZilObject ROUTINE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 3)
                throw new InterpreterError("ROUTINE", 3, 0);

            ZilAtom atom = args[0].Eval(ctx) as ZilAtom;
            if (atom == null)
                throw new InterpreterError("ROUTINE: first arg must be an atom");

            var oldAtom = ctx.ZEnvironment.InternGlobalName(atom);
            if (ctx.GetZVal(oldAtom) != null)
            {
                if (ctx.AllowRedefine)
                {
                    ctx.Redefine(oldAtom);
                    ctx.ZEnvironment.InternGlobalName(atom);
                }
                else
                    throw new InterpreterError("ROUTINE: already defined: " + oldAtom.ToStringContext(ctx, false));
            }

            ZilAtom activationAtom;
            ZilList argList;
            IEnumerable<ZilObject> body;

            if (args[1] is ZilAtom)
            {
                activationAtom = (ZilAtom)args[1];
                argList = args[2] as ZilList;
                if (argList == null || argList.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError("ROUTINE: third arg must be a list");
                body = args.Skip(3);
            }
            else if (args[1].GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
            {
                activationAtom = null;
                argList = (ZilList)args[1];
                body = args.Skip(2);
            }
            else
            {
                throw new InterpreterError("ROUTINE: second arg must be an atom or list");
            }

            var flags = CombineFlags(ctx.CurrentFileFlags, ctx.NextRoutineFlags);
            ctx.NextRoutineFlags = RoutineFlags.None;

            ZilRoutine rtn = new ZilRoutine(
                atom,
                activationAtom,
                argList,
                body,
                flags);

            var maxArgsAllowed = ctx.ZEnvironment.ZVersion > 3 ? 7 : 3;
            if (rtn.ArgSpec.MinArgCount > maxArgsAllowed)
            {
                throw new InterpreterError(
                    string.Format(
                        "ROUTINE: too many routine arguments: only {0} allowed in V{1}",
                        maxArgsAllowed,
                        ctx.ZEnvironment.ZVersion));
            }
            else if (rtn.ArgSpec.MaxArgCount > maxArgsAllowed)
            {
                var affectedArgCount = rtn.ArgSpec.MaxArgCount - maxArgsAllowed;
                Errors.TerpWarning(ctx, ctx.CallingForm.SourceLine,
                    "ROUTINE: only {0} routine arguments allowed in V{1}, so last {2} \"OPT\" argument{3} will never be passed",
                    maxArgsAllowed,
                    ctx.ZEnvironment.ZVersion,
                    affectedArgCount,
                    affectedArgCount == 1 ? "" : "s");
            }

            if (ctx.CallingForm != null)
                rtn.SourceLine = ctx.CallingForm.SourceLine;
            ctx.SetZVal(atom, rtn);
            ctx.ZEnvironment.Routines.Add(rtn);
            return atom;
        }

        private static RoutineFlags CombineFlags(FileFlags fileFlags, RoutineFlags routineFlags)
        {
            var result = routineFlags;

            if ((fileFlags & FileFlags.CleanStack) != 0)
                result |= RoutineFlags.CleanStack;

            return result;
        }

        [Subr]
        [Subr("MSETG")]
        public static ZilObject CONSTANT(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("CONSTANT", 2, 2);

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
            {
                var adecl = args[0] as ZilAdecl;
                if (adecl != null)
                    atom = adecl.First as ZilAtom;

                if (atom == null)
                    throw new InterpreterError("CONSTANT: first arg must be an atom (or ADECL'd atom)");
            }

            var oldAtom = ctx.ZEnvironment.InternGlobalName(atom);
            var previous = ctx.GetZVal(oldAtom);
            if (previous != null)
            {
                if (ctx.AllowRedefine)
                {
                    ctx.Redefine(oldAtom);
                    ctx.ZEnvironment.InternGlobalName(atom);
                }
                else if (previous is ZilConstant && ((ZilConstant)previous).Value.Equals(args[1]))
                {
                    // silently ignore duplicate constants as long as the values are equal
                    return previous;
                }
                else
                {
                    throw new InterpreterError("CONSTANT: already defined: " + oldAtom.ToStringContext(ctx, false));
                }
            }

            return ctx.AddZConstant(atom, args[1]);
        }

        [Subr]
        public static ZilObject GLOBAL(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            // typical form:  <GLOBAL atom-or-adecl default-value>
            // quirky form:   <GLOBAL atom-or-adecl default-value decl [size]>
            // TODO: use decl and size?
            if (args.Length < 2 || args.Length > 4)
                throw new InterpreterError("GLOBAL", 2, 4);

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
            {
                var adecl = args[0] as ZilAdecl;
                if (adecl != null)
                    atom = adecl.First as ZilAtom;

                if (atom == null)
                    throw new InterpreterError("GLOBAL: first arg must be an atom (or ADECL'd atom)");
            }

            var oldAtom = ctx.ZEnvironment.InternGlobalName(atom);
            var oldVal = ctx.GetZVal(oldAtom);
            if (oldVal != null)
            {
                if (ctx.AllowRedefine)
                {
                    if (oldVal is ZilGlobal)
                    {
                        var defaultValue = ((ZilGlobal)oldVal).Value;
                        if (defaultValue is ZilTable)
                        {
                            // prevent errors about duplicate symbol T?GLOBAL-NAME
                            // TODO: undefine the table if it hasn't been referenced anywhere yet
                            ((ZilTable)defaultValue).Name = null;
                        }
                    }

                    ctx.Redefine(oldAtom);
                    ctx.ZEnvironment.InternGlobalName(atom);
                }
                else
                    throw new InterpreterError("GLOBAL: already defined: " + oldAtom.ToStringContext(ctx, false));
            }

            if (args[1] is ZilTable)
                ((ZilTable)args[1]).Name = "T?" + atom.Text;

            ZilGlobal g = new ZilGlobal(atom, args[1]);
            ctx.SetZVal(atom, g);
            ctx.ZEnvironment.Globals.Add(g);
            return g;
        }

        [Subr("DEFINE-GLOBALS")]
        public static ZilObject DEFINE_GLOBALS(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 2)
                throw new InterpreterError("DEFINE-GLOBALS", 2, 0);

            for (int i = 1; i < args.Length; i++)
            {
                var spec = args[i] as ZilList;
                if (spec == null || spec.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError("DEFINE-GLOBALS: following arguments must be lists");

                var length = ((IStructure)spec).GetLength(3);
                if (length == null || length < 1)
                    throw new InterpreterError("DEFINE-GLOBALS: global spec must have 1 to 3 elements");

                var name = spec.First as ZilAtom;
                if (name == null)
                {
                    var nameAdecl = spec.First as ZilAdecl;
                    if (nameAdecl != null)
                        name = nameAdecl.First as ZilAtom;

                    if (name == null)
                        throw new InterpreterError("DEFINE-GLOBALS: global names must be atoms or ADECLs");
                }

                spec = spec.Rest;

                // BYTE/WORD (before default value)
                ZilAtom sizeAtom = null;
                if (!spec.IsEmpty)
                {
                    sizeAtom = spec.First as ZilAtom;
                    if (sizeAtom != null && (sizeAtom.StdAtom == StdAtom.BYTE || sizeAtom.StdAtom == StdAtom.WORD))
                    {
                        spec = spec.Rest;
                    }
                    else
                    {
                        sizeAtom = null;
                    }
                }

                // default value
                ZilObject defaultValue = null;
                if (!spec.IsEmpty)
                {
                    defaultValue = spec.First;
                    spec = spec.Rest;
                }

                // BYTE/WORD (after default value)
                if (sizeAtom == null && !spec.IsEmpty)
                {
                    sizeAtom = spec.First as ZilAtom;
                    if (sizeAtom != null && (sizeAtom.StdAtom == StdAtom.BYTE || sizeAtom.StdAtom == StdAtom.WORD))
                    {
                        spec = spec.Rest;
                    }
                    else
                    {
                        sizeAtom = null;
                    }
                }

                // create global and macros
                var globalAtom = ZilAtom.Parse("G?" + name, ctx);
                ZilGlobal g = new ZilGlobal(globalAtom, defaultValue ?? ctx.FALSE);
                if (sizeAtom != null && sizeAtom.StdAtom == StdAtom.BYTE)
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
        public static ZilObject OBJECT(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformObject(ctx, args, false);
        }

        [Subr]
        public static ZilObject ROOM(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformObject(ctx, args, true);
        }

        private static ZilObject PerformObject(Context ctx, ZilObject[] args, bool isRoom)
        {
            SubrContracts(ctx, args);

            string name = isRoom ? "ROOM" : "OBJECT";

            if (args.Length < 1)
                throw new InterpreterError(name, 1, 0);

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError(name + ": first arg must be an atom");

            var oldAtom = ctx.ZEnvironment.InternGlobalName(atom);
            if (ctx.GetZVal(oldAtom) != null)
            {
                if (ctx.AllowRedefine)
                {
                    ctx.Redefine(atom);
                    ctx.ZEnvironment.InternGlobalName(atom);
                }
                else
                    throw new InterpreterError(name + ": already defined: " + oldAtom.ToStringContext(ctx, false));
            }

            ZilList[] props = new ZilList[args.Length - 1];
            for (int i = 0; i < props.Length; i++)
            {
                props[i] = args[i + 1] as ZilList;
                if (props[i] == null || props[i].GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError(name + ": all property definitions must be lists");
            }

            ZilModelObject zmo = new ZilModelObject(atom, props, isRoom);
            if (ctx.CallingForm != null)
                zmo.SourceLine = ctx.CallingForm.SourceLine;
            ctx.SetZVal(atom, zmo);
            ctx.ZEnvironment.Objects.Add(zmo);
            return zmo;
        }

        [FSubr]
        public static ZilObject PROPDEF(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 2)
                throw new InterpreterError("PROPDEF", 2, 0);

            ZilAtom atom = args[0].Eval(ctx) as ZilAtom;
            if (atom == null)
                throw new InterpreterError("PROPDEF: first arg must be an atom");

            if (ctx.ZEnvironment.PropertyDefaults.ContainsKey(atom))
                Errors.TerpWarning(ctx, null,
                    "overriding default value for property '{0}'",
                    atom);

            ctx.ZEnvironment.PropertyDefaults[atom] = args[1].Eval(ctx);

            // complex property patterns
            if (args.Length >= 3)
            {
                var pattern = ComplexPropDef.Parse(args.Skip(2), ctx);
                ctx.SetPropDef(atom, pattern);
            }

            return atom;
        }

        [Subr]
        public static ZilObject ZSTART(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("ZSTART", 1, 1);

            var atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError("ZSTART: arg must be an atom");

            ctx.ZEnvironment.EntryRoutineName = atom;
            return args[0];
        }

        [Subr("BIT-SYNONYM")]
        public static ZilObject BIT_SYNONYM(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 2)
                throw new InterpreterError("BIT-SYNONYM", 2, 0);

            if (!args.All(a => a is ZilAtom))
                throw new InterpreterError("BIT-SYNONYM: all args must be atoms");

            var first = (ZilAtom)args[0];
            ZilAtom original;

            if (ctx.ZEnvironment.TryGetBitSynonym(first, out original))
                first = original;

            foreach (var synonym in args.Skip(1).Cast<ZilAtom>())
            {
                if (ctx.GetZVal(synonym) != null)
                    throw new InterpreterError("BIT-SYNONYM: symbol is already defined: " + synonym);

                ctx.ZEnvironment.AddBitSynonym(synonym, first);
            }

            return first;
        }

        #endregion

        #region Z-Code: Tables

        [Subr]
        public static ZilObject ITABLE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            // Syntax:
            //    <ITABLE [specifier] count [(flags...)] [init...]>
            // 'count' is a number of repetitions.
            // 'specifier' controls the length marker. BYTE specifier
            // makes the length marker a byte (but the table is still a
            // word table unless changed with a flag).
            // 'init' is a sequence of values to be repeated 'count' times.
            // values are compiled as words unless BYTE/LEXV flag is specified.

            if (args.Length < 1)
                throw new InterpreterError("ITABLE", 1, 0);

            int i = 0;
            TableFlags flags = 0;

            // optional specifier
            if (args[i] is ZilAtom)
            {
                switch (((ZilAtom)args[i]).StdAtom)
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
                        throw new InterpreterError("ITABLE: specifier must be NONE, BYTE, or WORD");
                }

                i++;
            }

            // element count
            ZilFix elemCount;
            if (i >= args.Length || (elemCount = args[i] as ZilFix) == null)
                throw new InterpreterError("ITABLE: missing element count");
            if (elemCount.Value < 1)
                throw new InterpreterError("ITABLE: invalid table size");
            i++;

            // optional flags
            if (i < args.Length && args[i] is ZilList)
            {
                bool gotLength = false;

                foreach (ZilObject obj in (ZilList)args[i])
                {
                    ZilAtom flag = obj as ZilAtom;
                    if (flag == null)
                        throw new InterpreterError("ITABLE: flags must be atoms");

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
                            // nada
                            break;
                        default:
                            throw new InterpreterError("ITABLE: unrecognized flag: " + flag);
                    }
                }

                if (gotLength)
                {
                    if ((flags & TableFlags.Byte) != 0)
                        flags |= TableFlags.ByteLength;
                    else
                        flags |= TableFlags.WordLength;
                }

                i++;
            }

            ZilObject[] initializer;
            if (i >= args.Length)
            {
                initializer = null;
            }
            else
            {
                initializer = new ZilObject[args.Length - i];
                Array.Copy(args, i, initializer, 0, initializer.Length);
            }

            ZilTable tab = new ZilTable(elemCount.Value, initializer, flags, null);
            if (ctx.CallingForm != null)
                tab.SourceLine = ctx.CallingForm.SourceLine;
            ctx.ZEnvironment.Tables.Add(tab);
            return tab;
        }

        private static ZilTable PerformTable(Context ctx, ZilObject[] args,
            bool pure, bool wantLength)
        {
            SubrContracts(ctx, args);

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

            int i = 0;
            if (args.Length > 0)
            {
                if (args[0].GetTypeAtom(ctx).StdAtom == StdAtom.LIST)
                {
                    i++;

                    var list = (ZilList)args[0];
                    while (!list.IsEmpty)
                    {
                        ZilAtom flag = list.First as ZilAtom;
                        if (flag == null)
                            throw new InterpreterError(name + ": flags must be atoms");

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
                            case StdAtom.PARSER_TABLE:
                                // nada
                                break;

                            case StdAtom.PATTERN:
                                list = list.Rest;
                                ZilList patternList;
                                if (list.IsEmpty || (patternList = list.First as ZilList) == null)
                                    throw new InterpreterError(name + ": expected a list after PATTERN");
                                pattern = patternList.ToArray();
                                ValidateTablePattern(name, pattern);
                                break;

                            default:
                                throw new InterpreterError(name + ": unrecognized flag: " + flag);
                        }

                        list = list.Rest;
                    }
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

            List<ZilObject> values = new List<ZilObject>(args.Length - i);
            while (i < args.Length)
            {
                ZilObject val = args[i];
                if (type == T_STRING && val.GetTypeAtom(ctx).StdAtom == StdAtom.STRING)
                {
                    string str = val.ToStringContext(ctx, true);
                    foreach (char c in str)
                        values.Add(new ZilFix(c));
                }
                else
                    values.Add(val);

                i++;
            }

            ZilTable tab = new ZilTable(
                1, values.ToArray(), flags, pattern == null ? null : pattern.ToArray());
            if (ctx.CallingForm != null)
                tab.SourceLine = ctx.CallingForm.SourceLine;
            ctx.ZEnvironment.Tables.Add(tab);
            return tab;
        }

        private static void ValidateTablePattern(string name, ZilObject[] pattern)
        {
            Contract.Requires(name != null);
            Contract.Requires(pattern != null && Contract.ForAll(pattern, p => p != null));

            if (pattern.Length == 0)
                throw new InterpreterError(name + ": PATTERN must not be empty");

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
                        throw new InterpreterError(name + ": vector may only appear at the end of a PATTERN");

                    if (vector.GetLength() < 2)
                        throw new InterpreterError(name + ": vector in PATTERN must have at least 2 elements");

                    // first element must be REST
                    var atom = vector[0] as ZilAtom;
                    if (atom == null || atom.StdAtom != StdAtom.REST)
                        throw new InterpreterError(name + ": vector in PATTERN must start with REST");

                    // remaining elements must be BYTE or WORD
                    if (!vector.Skip(1).All(zo => IsByteOrWordAtom(zo)))
                        throw new InterpreterError(name + ": following elements of vector in PATTERN must be BYTE or WORD");

                    // OK
                    continue;
                }

                throw new InterpreterError(name + ": PATTERN may only contain BYTE, WORD, or a REST vector");
            }
        }

        private static bool IsByteOrWordAtom(ZilObject value)
        {
            var atom = value as ZilAtom;
            return atom != null && (atom.StdAtom == StdAtom.BYTE || atom.StdAtom == StdAtom.WORD);
        }

        [Subr]
        public static ZilObject TABLE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformTable(ctx, args, false, false);
        }

        [Subr]
        public static ZilObject LTABLE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformTable(ctx, args, false, true);
        }

        [Subr]
        public static ZilObject PTABLE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformTable(ctx, args, true, false);
        }

        [Subr]
        public static ZilObject PLTABLE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformTable(ctx, args, true, true);
        }

        [Subr]
        public static ZilObject ZGET(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("ZGET", 2, 2);

            if (ctx.GetTypePrim(args[0].GetTypeAtom(ctx)) != PrimType.TABLE)
                throw new InterpreterError("ZGET: first arg must be a TABLE or derived type");

            var table = (ZilTable)args[0].GetPrimitive(ctx);

            var index = args[1] as ZilFix;
            if (index == null)
                throw new InterpreterError("ZGET: second arg must be a FIX");

            return table.GetWord(ctx, index.Value) ?? ctx.FALSE;
        }

        [Subr]
        public static ZilObject ZPUT(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 3)
                throw new InterpreterError("ZPUT", 3, 3);

            if (ctx.GetTypePrim(args[0].GetTypeAtom(ctx)) != PrimType.TABLE)
                throw new InterpreterError("ZPUT: first arg must be a TABLE or derived type");

            var table = (ZilTable)args[0].GetPrimitive(ctx);

            var index = args[1] as ZilFix;
            if (index == null)
                throw new InterpreterError("ZPUT: second arg must be a FIX");

            table.PutWord(ctx, index.Value, args[2]);
            return args[2];
        }

        [Subr]
        public static ZilObject GETB(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 2)
                throw new InterpreterError("GETB", 2, 2);

            if (ctx.GetTypePrim(args[0].GetTypeAtom(ctx)) != PrimType.TABLE)
                throw new InterpreterError("GETB: first arg must be a TABLE or derived type");

            var table = (ZilTable)args[0].GetPrimitive(ctx);

            var index = args[1] as ZilFix;
            if (index == null)
                throw new InterpreterError("GETB: second arg must be a FIX");

            return table.GetByte(ctx, index.Value) ?? ctx.FALSE;
        }

        [Subr]
        public static ZilObject PUTB(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 3)
                throw new InterpreterError("PUTB", 3, 3);

            if (ctx.GetTypePrim(args[0].GetTypeAtom(ctx)) != PrimType.TABLE)
                throw new InterpreterError("PUTB: first arg must be a TABLE or derived type");

            var table = (ZilTable)args[0].GetPrimitive(ctx);

            var index = args[1] as ZilFix;
            if (index == null)
                throw new InterpreterError("PUTB: second arg must be a FIX");

            var value = args[2];
            switch (value.GetTypeAtom(ctx).StdAtom)
            {
                case StdAtom.FIX:
                case StdAtom.BYTE:
                    // OK
                    break;

                default:
                    throw new InterpreterError("PUTB: third arg must be a FIX or BYTE");
            }

            table.PutByte(ctx, index.Value, value);
            return value;
        }

        #endregion

        #region Z-Code: Version, Options, Capabilities

        [Subr]
        public static ZilObject VERSION(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError("VERSION", 1, 2);

            int newVersion = ParseZVersion("VERSION", args[0]);

            ctx.SetZVersion(newVersion);

            if (args.Length > 1)
            {
                var atom = args[1] as ZilAtom;
                if (atom == null || atom.StdAtom != StdAtom.TIME)
                    throw new InterpreterError("VERSION: second arg must be the atom TIME");

                if (ctx.ZEnvironment.ZVersion != 3)
                    throw new InterpreterError("VERSION: TIME is only meaningful in version 3");

                ctx.ZEnvironment.TimeStatusLine = true;
            }

            return new ZilFix(newVersion);
        }

        private static int ParseZVersion(string name, ZilObject expr)
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
                        throw new InterpreterError(name + ": unrecognized version name (must be ZIP, EZIP, XZIP, YZIP)");
                }
            }
            else if (expr is ZilFix)
            {
                newVersion = ((ZilFix)expr).Value;
                if (newVersion < 3 || newVersion > 8)
                    throw new InterpreterError(name + ": version number out of range (must be 3-6)");
            }
            else
            {
                throw new InterpreterError(name + ": arg must be an atom or a FIX");
            }
            return newVersion;
        }

        [Subr("CHECK-VERSION?")]
        public static ZilObject CHECK_VERSION_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("CHECK-VERSION?", 1, 1);

            int version = ParseZVersion("CHECK-VERSION?", args[0]);
            return ctx.ZEnvironment.ZVersion == version ? ctx.TRUE : ctx.FALSE;
        }

        [FSubr("VERSION?")]
        public static ZilObject VERSION_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1)
                throw new InterpreterError("VERSION?", 1, 0);

            ZilAtom tAtom = ctx.GetStdAtom(StdAtom.T);
            ZilAtom elseAtom = ctx.GetStdAtom(StdAtom.ELSE);

            foreach (ZilObject clause in args)
            {
                ZilList list = clause as ZilList;
                if (list == null || list.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                    throw new InterpreterError("VERSION?: args must be lists");

                if (list.IsEmpty)
                    throw new InterpreterError("VERSION?: lists must be non-empty");

                if (list.First == tAtom || list.First == elseAtom ||
                    ParseZVersion("VERSION?", list.First) == ctx.ZEnvironment.ZVersion)
                {
                    ZilObject result = list.First;
                    foreach (ZilObject expr in list.Rest)
                        result = expr.Eval(ctx);
                    return result;
                }
            }

            return ctx.FALSE;
        }

        [Subr("ORDER-OBJECTS?")]
        public static ZilObject ORDER_OBJECTS_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1)
                throw new InterpreterError("ORDER-OBJECTS?", 1, 0);

            if (args[0] is ZilAtom)
            {
                switch (((ZilAtom)args[0]).StdAtom)
                {
                    case StdAtom.DEFINED:
                        ctx.ZEnvironment.ObjectOrdering = ObjectOrdering.Defined;
                        return args[0];
                    case StdAtom.ROOMS_FIRST:
                        ctx.ZEnvironment.ObjectOrdering = ObjectOrdering.RoomsFirst;
                        return args[0];
                    case StdAtom.ROOMS_AND_LGS_FIRST:
                        ctx.ZEnvironment.ObjectOrdering = ObjectOrdering.RoomsAndLocalGlobalsFirst;
                        return args[0];
                    case StdAtom.ROOMS_LAST:
                        ctx.ZEnvironment.ObjectOrdering = ObjectOrdering.RoomsLast;
                        return args[0];
                }
            }

            throw new InterpreterError("ORDER-OBJECTS?: first arg must be DEFINED, ROOMS-FIRST, ROOMS-AND-LGS-FIRST, or ROOMS-LAST");
        }

        [Subr("ORDER-TREE?")]
        public static ZilObject ORDER_TREE_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("ORDER-TREE?", 1, 1);

            if (args[0] is ZilAtom)
            {
                switch (((ZilAtom)args[0]).StdAtom)
                {
                    case StdAtom.REVERSE_DEFINED:
                        ctx.ZEnvironment.TreeOrdering = TreeOrdering.ReverseDefined;
                        return args[0];
                }
            }

            throw new InterpreterError("ORDER-TREE?: first arg must be REVERSE-DEFINED");
        }

        [Subr("ORDER-FLAGS?")]
        public static ZilObject ORDER_FLAGS_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 2)
                throw new InterpreterError("ORDER-FLAGS?", 2, 0);

            var atom = args[0] as ZilAtom;
            if (atom == null || atom.StdAtom != StdAtom.LAST)
                throw new InterpreterError("ORDER-FLAGS?: first arg must be LAST");

            for (int i = 1; i < args.Length; i++)
            {
                atom = args[i] as ZilAtom;
                if (atom == null)
                    throw new InterpreterError("ORDER-FLAGS?: all args must be atoms");

                ctx.ZEnvironment.FlagsOrderedLast.Add(atom);
            }

            return args[0];
        }

        [Subr("ZIP-OPTIONS")]
        public static ZilObject ZIP_OPTIONS(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            foreach (var arg in args)
            {
                var atom = arg as ZilAtom;
                if (atom == null)
                    throw new InterpreterError("ZIP-OPTIONS: all args must be atoms");

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

                    default:
                        throw new InterpreterError("ZIP-OPTIONS: unrecognized option " + atom);
                }

                ctx.DefineCompilationFlag(atom, ctx.TRUE, true);
                ctx.SetGlobalVal(ctx.GetStdAtom(flag), ctx.TRUE);
            }

            return ctx.TRUE;
        }

        [Subr("FREQUENT-WORDS?")]
        public static ZilObject FREQUENT_WORDS_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            // nada - we always generate frequent words
            return ctx.TRUE;
        }

        [Subr("LONG-WORDS?")]
        public static ZilObject LONG_WORDS_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            ctx.ZEnvironment.GenerateLongWords = true;
            return ctx.TRUE;
        }

        [Subr("FUNNY-GLOBALS?")]
        public static ZilObject FUNNY_GLOBALS_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            ctx.SetGlobalVal(ctx.GetStdAtom(StdAtom.DO_FUNNY_GLOBALS_P), ctx.TRUE);
            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject CHRSET(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 2)
                throw new InterpreterError("CHRSET", 2, 0);

            var fix = args[0] as ZilFix;
            if (fix == null)
                throw new InterpreterError("CHRSET: first arg must be a FIX");

            var alphabetNum = fix.Value;
            if (alphabetNum < 0 || alphabetNum > 2)
                throw new InterpreterError("CHRSET: alphabet number must be between 0 and 2");

            var sb = new StringBuilder(26);

            foreach (var item in args.Skip(1))
            {
                var primitive = item.GetPrimitive(ctx);
                switch (item.GetTypeAtom(ctx).StdAtom)
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
                        throw new InterpreterError("CHRSET: alphabet components must be STRING, CHARACTER, FIX, or BYTE");
                }
            }

            var alphabetStr = sb.ToString();
            int requiredLen = (alphabetNum == 2) ? 24 : 26;
            if (alphabetStr.Length != requiredLen)
                throw new InterpreterError(string.Format("CHRSET: alphabet {0} needs {1} characters", alphabetNum, requiredLen));

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

            return new ZilString(alphabetStr);
        }

        [Subr]
        public static ZilObject LANGUAGE(Context ctx, ZilObject[] args)
        {
            if (args.Length < 1 || args.Length > 3)
                throw new InterpreterError("LANGUAGE", 1, 3);

            var name = args[0] as ZilAtom;
            if (name == null)
                throw new InterpreterError("LANGUAGE: first arg must be an atom");

            var language = ZModel.Language.Get(name.Text);
            if (language == null)
                throw new InterpreterError("LANGUAGE: unrecognized language: " + name.Text);

            char escapeChar;
            if (args.Length >= 2)
            {
                var ch = args[1] as ZilChar;
                if (ch == null)
                    throw new InterpreterError("LANGUAGE: second arg must be a CHARACTER");
                escapeChar = ch.Char;
            }
            else
            {
                escapeChar = '%';
            }

            bool changeChrset;
            if (args.Length >= 3)
            {
                changeChrset = args[2].IsTrue;
            }
            else
            {
                changeChrset = true;
            }

            // update language, escape char, and possibly charset
            ctx.ZEnvironment.Language = language;
            ctx.ZEnvironment.LanguageEscapeChar = escapeChar;

            if (changeChrset)
            {
                ctx.ZEnvironment.Charset0 = language.Charset0;
                ctx.ZEnvironment.Charset1 = language.Charset1;
                ctx.ZEnvironment.Charset2 = language.Charset2;
            }

            return args[0];
        }

        #endregion

        #region Z-Code: Vocabulary and Syntax

        [Subr]
        public static ZilObject DIRECTIONS(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length == 0)
                throw new InterpreterError("DIRECTIONS", 1, 0);

            if (!args.All(zo => zo is ZilAtom))
                throw new InterpreterError("DIRECTIONS: all args must be atoms");

            // if a PROPSPEC is set for DIRECTIONS, it'll be copied to the new direction properties
            var propspecAtom = ctx.GetStdAtom(StdAtom.PROPSPEC);
            var propspec = ctx.GetProp(ctx.GetStdAtom(StdAtom.DIRECTIONS), propspecAtom);

            ctx.ZEnvironment.Directions.Clear();
            foreach (ZilAtom arg in args)
            {
                ctx.ZEnvironment.Directions.Add(arg);
                ctx.ZEnvironment.GetVocabDirection(arg, ctx.CallingForm.SourceLine);
                ctx.ZEnvironment.LowDirection = arg;

                ctx.PutProp(arg, propspecAtom, propspec);
            }

            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject BUZZ(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length == 0)
                throw new InterpreterError("BUZZ", 1, 0);

            if (!args.All(zo => zo is ZilAtom))
                throw new InterpreterError("BUZZ: all args must be atoms");

            foreach (ZilAtom arg in args)
                ctx.ZEnvironment.Buzzwords.Add(new KeyValuePair<ZilAtom, ISourceLine>(arg, ctx.CallingForm.SourceLine));

            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject VOC(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 1 || args.Length > 2)
                throw new InterpreterError("VOC", 1, 2);

            var text = args[0] as ZilString;
            if (text == null)
                throw new InterpreterError("VOC: first arg must be a string");

            var atom = ZilAtom.Parse(text.Text, ctx);
            var word = ctx.ZEnvironment.GetVocab(atom);

            if (args.Length > 1 && !(args[1] is ZilFalse))
            {
                var type = args[1] as ZilAtom;
                if (type == null)
                    throw new InterpreterError("VOC: second arg must be FALSE or an atom");

                switch (type.StdAtom)
                {
                    case StdAtom.ADJ:
                    case StdAtom.ADJECTIVE:
                        word = ctx.ZEnvironment.GetVocabAdjective(atom, ctx.CallingForm.SourceLine);
                        break;

                    case StdAtom.NOUN:
                    case StdAtom.OBJECT:
                        word = ctx.ZEnvironment.GetVocabNoun(atom, ctx.CallingForm.SourceLine);
                        break;

                    case StdAtom.BUZZ:
                        word = ctx.ZEnvironment.GetVocabBuzzword(atom, ctx.CallingForm.SourceLine);
                        break;

                    case StdAtom.PREP:
                        word = ctx.ZEnvironment.GetVocabPreposition(atom, ctx.CallingForm.SourceLine);
                        break;

                    case StdAtom.DIR:
                        word = ctx.ZEnvironment.GetVocabDirection(atom, ctx.CallingForm.SourceLine);
                        break;

                    case StdAtom.VERB:
                        word = ctx.ZEnvironment.GetVocabVerb(atom, ctx.CallingForm.SourceLine);
                        break;

                    default:
                        throw new InterpreterError("VOC: unrecognized part of speech: " + type);
                }
            }

            return ctx.ChangeType(atom, ctx.GetStdAtom(StdAtom.VOC));
        }

        [Subr]
        public static ZilObject SYNTAX(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length < 3)
                throw new InterpreterError("SYNTAX", 3, 0);

            Syntax syntax = Syntax.Parse(ctx.CallingForm.SourceLine, args, ctx);
            ctx.ZEnvironment.Syntaxes.Add(syntax);

            if (syntax.Synonyms.Count > 0)
            {
                var synonymArgs = Enumerable.Repeat(syntax.Verb.Atom, 1).Concat(syntax.Synonyms).ToArray();
                PerformSynonym(ctx, synonymArgs, "SYNTAX (verb synonyms)", typeof(VerbSynonym));
            }

            return syntax.Verb.Atom;
        }

        private static ZilObject PerformSynonym(Context ctx, ZilObject[] args,
            string name, Type synonymType)
        {
            SubrContracts(ctx, args);
            Contract.Requires(name != null);
            Contract.Requires(synonymType != null);

            if (args.Length < 1)
                throw new InterpreterError(name, 1, 0);

            const string STypeError = ": args must be atoms";

            ZilAtom atom = args[0] as ZilAtom;
            if (atom == null)
                throw new InterpreterError(name + STypeError);

            IWord oldWord;
            if (ctx.ZEnvironment.Vocabulary.TryGetValue(atom, out oldWord) == false)
            {
                oldWord = ctx.ZEnvironment.VocabFormat.CreateWord(atom);
                ctx.ZEnvironment.Vocabulary.Add(atom, oldWord);
            }

            object[] ctorArgs = new object[2];
            ctorArgs[0] = oldWord;

            for (int i = 1; i < args.Length; i++)
            {
                atom = args[i] as ZilAtom;
                if (atom == null)
                    throw new InterpreterError(name + STypeError);

                IWord newWord;
                if (ctx.ZEnvironment.Vocabulary.TryGetValue(atom, out newWord) == false)
                {
                    newWord = ctx.ZEnvironment.VocabFormat.CreateWord(atom);
                    ctx.ZEnvironment.Vocabulary.Add(atom, newWord);
                }

                ctorArgs[1] = newWord;
                ctx.ZEnvironment.Synonyms.Add((Synonym)Activator.CreateInstance(
                    synonymType, ctorArgs));
            }

            return atom;
        }

        [Subr]
        public static ZilObject SYNONYM(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformSynonym(ctx, args, "SYNONYM", typeof(Synonym));
        }

        [Subr("VERB-SYNONYM")]
        public static ZilObject VERB_SYNONYM(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformSynonym(ctx, args, "VERB-SYNONYM", typeof(VerbSynonym));
        }

        [Subr("PREP-SYNONYM")]
        public static ZilObject PREP_SYNONYM(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformSynonym(ctx, args, "PREP-SYNONYM", typeof(PrepSynonym));
        }

        [Subr("ADJ-SYNONYM")]
        public static ZilObject ADJ_SYNONYM(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformSynonym(ctx, args, "ADJ-SYNONYM", typeof(AdjSynonym));
        }

        [Subr("DIR-SYNONYM")]
        public static ZilObject DIR_SYNONYM(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return PerformSynonym(ctx, args, "DIR-SYNONYM", typeof(DirSynonym));
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
    }
}
