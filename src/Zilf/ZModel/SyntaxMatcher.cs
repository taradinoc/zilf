/* Copyright 2010-2025 Tara McGrew
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

using Zilf.Diagnostics;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.ZModel.Vocab;

namespace Zilf.ZModel
{
    /// <summary>
    /// Matches syntax definitions based on patterns for verb, prepositions, objects, and actions.
    /// Used by REMOVE-SYNTAX to filter syntax definitions.
    /// </summary>
    /// <remarks>
    /// Pattern syntax:
    /// <list type="bullet">
    /// <item><description><c>*</c> - matches any value or nothing</description></item>
    /// <item><description><c>&lt;&gt;</c> - matches nothing (absence of a value)</description></item>
    /// <item><description>atom - matches that specific atom</description></item>
    /// </list>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    /// <item><description><c>&lt;REMOVE-SYNTAX TAKE&gt;</c> - matches TAKE, TAKE INVENTORY, TAKE OBJECT FROM OBJECT</description></item>
    /// <item><description><c>&lt;REMOVE-SYNTAX PUT * * OBJECT&gt;</c> - matches PUT OBJECT IN OBJECT, PUT OBJECT ON OBJECT</description></item>
    /// <item><description><c>&lt;REMOVE-SYNTAX PUT OBJECT IN&gt;</c> - matches PUT OBJECT IN OBJECT</description></item>
    /// <item><description><c>&lt;REMOVE-SYNTAX * = V-TAKE-FROM&gt;</c> - matches any verb syntax with action V-TAKE-FROM</description></item>
    /// <item><description><c>&lt;REMOVE-SYNTAX *&gt;</c> - matches all syntaxes</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    sealed class SyntaxMatcher
    {
        enum PatternType
        {
            Any,        // * - matches any value or nothing
            Nothing,    // <> - matches nothing
            Specific    // atom - matches specific value
        }

        readonly struct Pattern(PatternType type, ZilAtom? atom = null)
        {
            public bool Matches(ZilAtom? value) => type switch
            {
                PatternType.Any => true,
                PatternType.Nothing => value == null,
                PatternType.Specific => atom?.Equals(value) ?? (value == null),
                _ => false
            };

            public bool Matches(IWord? word) => Matches(word?.Atom);
        }

        readonly Pattern verbPattern;
        readonly Pattern prep1Pattern;
        readonly Pattern prep2Pattern;
        readonly Pattern actionPattern;
        readonly bool matchObj1;      // true if pattern specifies OBJECT for first slot
        readonly bool matchObj2;      // true if pattern specifies OBJECT for second slot

        public SyntaxMatcher(ZilObject[] args)
        {
            // Parse: verb-pattern [[prep-pattern] OBJECT] [[prep-pattern] OBJECT] [= action-pattern]
            // Default to matching any
            verbPattern = new Pattern(PatternType.Any);
            prep1Pattern = new Pattern(PatternType.Any);
            prep2Pattern = new Pattern(PatternType.Any);
            actionPattern = new Pattern(PatternType.Any);
            matchObj1 = false;
            matchObj2 = false;

            if (args.Length == 0)
                return;

            int index = 0;

            // Parse verb pattern
            verbPattern = ParsePattern(args[index]);
            index++;

            // Track whether we're looking for prep or OBJECT
            Pattern? pendingPrep = null;
            int objSlot = 0; // 0 = none, 1 = first, 2 = second

            while (index < args.Length)
            {
                var arg = args[index];

                // Check for = (action separator)
                if (IsEqualSign(arg))
                {
                    index++;
                    break;
                }

                // Check for OBJECT keyword
                if (IsObjectKeyword(arg))
                {
                    objSlot++;
                    if (objSlot == 1)
                    {
                        matchObj1 = true;
                        prep1Pattern = pendingPrep ?? new Pattern(PatternType.Any);
                        pendingPrep = null;
                    }
                    else if (objSlot == 2)
                    {
                        matchObj2 = true;
                        prep2Pattern = pendingPrep ?? new Pattern(PatternType.Any);
                        pendingPrep = null;
                    }
                    index++;
                    continue;
                }

                // It's a prep pattern (or could be <>)
                var pattern = ParsePattern(arg);

                // If we have a pending prep already, the previous one goes to the current slot
                if (pendingPrep.HasValue)
                {
                    objSlot++;
                    if (objSlot == 1)
                    {
                        prep1Pattern = pendingPrep.Value;
                        // No matchObj1 set means we don't care about object presence
                    }
                    else if (objSlot == 2)
                    {
                        prep2Pattern = pendingPrep.Value;
                    }
                }

                pendingPrep = pattern;
                index++;
            }

            // Handle any remaining prep pattern
            if (pendingPrep.HasValue)
            {
                objSlot++;
                if (objSlot == 1)
                {
                    prep1Pattern = pendingPrep.Value;
                }
                else if (objSlot == 2)
                {
                    prep2Pattern = pendingPrep.Value;
                }
            }

            // Parse action pattern (if we found =)
            if (index < args.Length)
            {
                actionPattern = ParsePattern(args[index]);
            }
        }

        static Pattern ParsePattern(ZilObject arg)
        {
            switch (arg)
            {
                case ZilFalse:
                    return new Pattern(PatternType.Nothing);

                case ZilAtom { StdAtom: StdAtom.Times }:
                    return new Pattern(PatternType.Any);

                case ZilAtom atom:
                    return new Pattern(PatternType.Specific, atom);

                default:
                    throw new InterpreterError(InterpreterMessages.Unrecognized_Token_In_Syntax_Pattern_0, arg);
            }
        }

        static bool IsObjectKeyword(ZilObject obj) => obj is ZilAtom { StdAtom: StdAtom.OBJECT };

        static bool IsEqualSign(ZilObject obj) => obj is ZilAtom { StdAtom: StdAtom.Eq };

        public bool Matches(Syntax syntax)
        {
            // Match verb
            if (!verbPattern.Matches(syntax.Verb))
                return false;

            // Match object count constraints
            if (matchObj1 && syntax.NumObjects < 1)
                return false;
            if (matchObj2 && syntax.NumObjects < 2)
                return false;

            // Match first prep (can be set even when numObjects == 0)
            if (!prep1Pattern.Matches(syntax.Preposition1))
                return false;

            // Match second prep (only check if syntax has 2 objects)
            if (syntax.NumObjects >= 2 && !prep2Pattern.Matches(syntax.Preposition2))
                return false;

            // Match action
            if (!actionPattern.Matches(syntax.Action))
                return false;

            return true;
        }
    }
}