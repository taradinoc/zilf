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
using System.Runtime.CompilerServices;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.ZModel.Vocab
{
    static class ScopeFlags
    {
        public static class Original
        {
            public const byte Have = 2;
            public const byte Many = 4;
            public const byte Take = 8;
            public const byte OnGround = 16;
            public const byte InRoom = 32;
            public const byte Carried = 64;
            public const byte Held = 128;

            public const byte Default = OnGround | InRoom | Carried | Held;
        }

        class CacheEntry
        {
            /// <summary>
            /// A map from scope flag names to their bit values.
            /// </summary>
            public readonly Dictionary<string, byte> Dict = new Dictionary<string, byte>();
            /// <summary>
            /// The names of scope flags that add to the defaults instead of overriding (if only additive flags are given).
            /// </summary>
            public readonly HashSet<string> Additive = new HashSet<string>();
            /// <summary>
            /// The default set of flags to use when no scope flags are given.
            /// </summary>
            /// <remarks>
            /// If only <see cref="Additive"/> flags are given, they will combine with the defaults instead of replacing.
            /// </remarks>
            public byte DefaultFlags;
        }

        static ConditionalWeakTable<ZilVector, CacheEntry> sflagsCache = new ConditionalWeakTable<ZilVector, CacheEntry>();

        public static byte Parse(ZilList list, Context ctx)
        {
            byte result = 0;
            var sflagsVector = ctx.GetGlobalVal(ctx.GetStdAtom(StdAtom.NEW_SFLAGS)) as ZilVector;

            if (sflagsVector == null)
            {
                // use default set of flags
                if (list == null)
                    return Original.Default;

                foreach (var obj in list)
                {
                    var atom = obj as ZilAtom;
                    if (atom == null)
                        throw new InterpreterError(InterpreterMessages.Object_Options_In_Syntax_Must_Be_Atoms);

                    switch (atom.StdAtom)
                    {
                        case StdAtom.TAKE:
                            result |= Original.Take;
                            break;
                        case StdAtom.HAVE:
                            result |= Original.Have;
                            break;
                        case StdAtom.MANY:
                            result |= Original.Many;
                            break;
                        case StdAtom.HELD:
                            result |= Original.Held;
                            break;
                        case StdAtom.CARRIED:
                            result |= Original.Carried;
                            break;
                        case StdAtom.ON_GROUND:
                            result |= Original.OnGround;
                            break;
                        case StdAtom.IN_ROOM:
                            result |= Original.InRoom;
                            break;
                        default:
                            throw new InterpreterError(InterpreterMessages.Unrecognized_Object_Option_0, atom.ToString());
                    }
                }
            }
            else
            {
                // use custom flags
                CacheEntry entry;
                if (sflagsCache.TryGetValue(sflagsVector, out entry) == false)
                {
                    var length = sflagsVector.GetLength();
                    if (length % 2 != 0)
                        throw new InterpreterError(InterpreterMessages.NEWSFLAGS_Vector_Must_Have_An_Even_Number_Of_Elements);

                    entry = new CacheEntry();

                    // default set of flags
                    entry.DefaultFlags = GetSflagValue(ctx, StdAtom.SEARCH_ALL);

                    // additive flags: always present
                    MakeAdditiveFlag(ctx, entry, "HAVE", StdAtom.SEARCH_MUST_HAVE);
                    MakeAdditiveFlag(ctx, entry, "TAKE", StdAtom.SEARCH_DO_TAKE);
                    MakeAdditiveFlag(ctx, entry, "MANY", StdAtom.SEARCH_MANY);

                    // user-defined flags
                    for (int i = 0; i < length; i += 2)
                    {
                        var name = sflagsVector[i];
                        var value = sflagsVector[i + 1];

                        string nameStr;
                        if (name is ZilString)
                            nameStr = ((ZilString)name).Text;
                        else if (name is ZilAtom)
                            nameStr = ((ZilAtom)name).Text;
                        else
                            throw new InterpreterError(InterpreterMessages.NEWSFLAGS_Names_Must_Be_Strings_Or_Atoms);

                        if (!(value is ZilFix) || (((ZilFix)value).Value & ~255) != 0)
                            throw new InterpreterError(InterpreterMessages.NEWSFLAGS_Values_Must_Be_FIXes_Between_0_And_255);

                        entry.Dict[nameStr] = (byte)((ZilFix)value).Value;
                    }
                }

                if (list == null)
                    return entry.DefaultFlags;

                bool cleared = false;
                result = entry.DefaultFlags;

                foreach (var obj in list)
                {
                    var atom = obj as ZilAtom;
                    if (atom == null)
                        throw new InterpreterError(InterpreterMessages.Object_Options_In_Syntax_Must_Be_Atoms);

                    string name = atom.Text;
                    byte value;

                    if (!entry.Dict.TryGetValue(name, out value))
                        throw new InterpreterError(InterpreterMessages.Unrecognized_Object_Option_NEWSFLAGS_Used_0, name);

                    if (!cleared && !entry.Additive.Contains(name))
                    {
                        cleared = true;
                        result &= (byte)(~entry.DefaultFlags);
                    }

                    result |= value;
                }
            }

            return result;
        }

        static byte GetSflagValue(Context ctx, StdAtom stdAtom)
        {
            var atom = ctx.GetStdAtom(stdAtom);
            var gval = ctx.GetGlobalVal(atom);
            if (gval == null)
                throw new InterpreterError(InterpreterMessages._0_Must_Have_A_GVAL_To_Use_NEWSFLAGS, atom.ToStringContext(ctx, false));

            var fix = gval as ZilFix;
            if (fix == null)
                throw new InterpreterError(InterpreterMessages.GVAL_Of_0_Must_Be_A_FIX, atom.ToStringContext(ctx, false));

            if ((fix.Value & ~255) != 0)
                throw new InterpreterError(InterpreterMessages.GVAL_Of_0_Must_Be_Between_0_And_255, atom.ToStringContext(ctx, false));

            return (byte)fix.Value;
        }

        static void MakeAdditiveFlag(Context ctx, CacheEntry entry, string name, StdAtom valueAtom)
        {
            entry.Dict.Add(name, GetSflagValue(ctx, valueAtom));
            entry.Additive.Add(name);
        }
    }
}