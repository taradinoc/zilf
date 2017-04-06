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
using System.Runtime.CompilerServices;
using Zilf.Interpreter;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;
using System.Linq;

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
                    if (!(obj is ZilAtom atom))
                        throw new InterpreterError(InterpreterMessages._0_In_1_Must_Be_2, "object options", "SYNTAX", "atoms");

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
                            throw new InterpreterError(InterpreterMessages.Unrecognized_0_1, "object option", atom.ToString());
                    }
                }
            }
            else
            {
                // use custom flags
                if (sflagsCache.TryGetValue(sflagsVector, out var entry) == false)
                {
                    var length = sflagsVector.GetLength();
                    if (length % 2 != 0)
                        throw new InterpreterError(
                            InterpreterMessages._0_Must_Have_1_Element1s,
                            "NEW-SFLAGS vector",
                            new CountableString("an even number of", true));

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
                        if (name is ZilString zstr)
                            nameStr = zstr.Text;
                        else if (name is ZilAtom atom)
                            nameStr = atom.Text;
                        else
                            throw new InterpreterError(
                                InterpreterMessages._0_Must_Be_1,
                                "NEW-SFLAGS names",
                                "strings or atoms");

                        if (value is ZilFix fix && (fix.Value & ~255) == 0)
                        {
                            entry.Dict[nameStr] = (byte)fix.Value;
                        }
                        else
                        {
                            throw new InterpreterError(
                                InterpreterMessages._0_Must_Be_1,
                                "NEW-SFLAGS values",
                                "FIXes between 0 and 255");
                        }
                    }
                }

                if (list == null)
                    return entry.DefaultFlags;

                bool cleared = false;
                result = entry.DefaultFlags;

                foreach (var obj in list)
                {
                    if (!(obj is ZilAtom atom))
                        throw new InterpreterError(InterpreterMessages._0_In_1_Must_Be_2, "object options", "SYNTAX", "atoms");

                    string name = atom.Text;

                    if (!entry.Dict.TryGetValue(name, out var value))
                        throw new InterpreterError(InterpreterMessages.Unrecognized_0_1, "object option", name)
                            .Combine(new InterpreterError(
                                InterpreterMessages.Since_NEWSFLAGS_Is_Set_The_Following_Options_Are_Recognized_0,
                                string.Join(", ", entry.Dict.Keys.OrderBy(s => s))));

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

            if (!(gval is ZilFix fix))
                throw new InterpreterError(
                    InterpreterMessages._0_Value_Of_1_Must_Be_2,
                    "global",
                    atom.ToStringContext(ctx, false),
                    "a FIX");

            if ((fix.Value & ~255) != 0)
                throw new InterpreterError(
                    InterpreterMessages._0_Value_Of_1_Must_Be_2, 
                    "global",
                    atom.ToStringContext(ctx, false),
                    "between 0 and 255");

            return (byte)fix.Value;
        }

        static void MakeAdditiveFlag(Context ctx, CacheEntry entry, string name, StdAtom valueAtom)
        {
            entry.Dict.Add(name, GetSflagValue(ctx, valueAtom));
            entry.Additive.Add(name);
        }
    }
}