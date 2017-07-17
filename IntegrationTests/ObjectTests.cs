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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace IntegrationTests
{
    [TestClass]
    public class ObjectTests
    {
        static GlobalsAssertionHelper AssertGlobals(params string[] globals)
        {
            Contract.Requires(globals != null && globals.Length > 0);
            Contract.Requires(Contract.ForAll(globals, c => !string.IsNullOrWhiteSpace(c)));

            return new GlobalsAssertionHelper(globals);
        }

        static RoutineAssertionHelper AssertRoutine(string argSpec, string body)
        {
            Contract.Requires(argSpec != null);
            Contract.Requires(!string.IsNullOrWhiteSpace(body));

            return new RoutineAssertionHelper(argSpec, body);
        }

        #region Object Numbering & Tree Ordering

        string[] TreeImplications(string[] numbering, params string[][] chains)
        {
            Contract.Requires(numbering != null && numbering.Length > 0);
            Contract.Requires(Contract.ForAll(numbering, n => !string.IsNullOrWhiteSpace(n)));
            Contract.Requires(Contract.ForAll(chains, c => c.Length >= 2));

            var result = new List<string>();

            for (int i = 0; i < numbering.Length; i++)
            {
                result.Add(string.Format("<=? ,{0} {1}>", numbering[i], i + 1));
            }

            var heads = new HashSet<string>();

            if (chains != null)
            {
                foreach (var chain in chains)
                {
                    Contract.Assert(chain.Length >= 2);

                    heads.Add(chain[0]);
                    result.Add(string.Format("<=? <FIRST? ,{0}> ,{1}>", chain[0], chain[1]));

                    for (int i = 1; i < chain.Length - 1; i++)
                    {
                        result.Add(string.Format("<=? <NEXT? ,{0}> ,{1}>", chain[i], chain[i + 1]));
                    }

                    result.Add(string.Format("<NOT <NEXT? ,{0}>>", chain[chain.Length - 1]));
                }
            }

            foreach (var o in numbering)
            {
                if (!heads.Contains(o))
                {
                    result.Add(string.Format("<NOT <FIRST? ,{0}>>", o));
                }
            }

            return result.ToArray();
        }

        /* Default ordering:
         * 
         * Objects are numbered in reverse definition-or-mention order.
         * 
         * The object tree is traversed in reverse definition order (ignoring mere mentions),
         * except that the first child defined is the first child traversed (not the last).
         */

        [TestMethod]
        public void TestContents_DefaultOrder()
        {
            AssertGlobals(
                "<OBJECT RAINBOW>",
                "<OBJECT RED (IN RAINBOW)>",
                "<OBJECT YELLOW (IN RAINBOW)>",
                "<OBJECT GREEN (IN RAINBOW)>",
                "<OBJECT BLUE (IN RAINBOW)>")
                .Implies(TreeImplications(
                    new[] { "BLUE", "GREEN", "YELLOW", "RED", "RAINBOW" },
                    new[] { "RAINBOW", "RED", "BLUE", "GREEN", "YELLOW" }));
        }

        [TestMethod]
        public void TestHouse_DefaultOrder()
        {
            AssertGlobals(
                "<OBJECT FRIDGE (IN KITCHEN)>",
                "<OBJECT SINK (IN KITCHEN)>",
                "<OBJECT MICROWAVE (IN KITCHEN)>",
                "<ROOM KITCHEN (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT FLOOR (IN LOCAL-GLOBALS)>",
                "<ROOM BEDROOM (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT BED (IN BEDROOM)>",
                "<OBJECT ROOMS>",
                "<OBJECT LOCAL-GLOBALS>",
                "<OBJECT CEILING (IN LOCAL-GLOBALS)>")
                .Implies(TreeImplications(
                    new[] { "BED", "BEDROOM", "LOCAL-GLOBALS", "CEILING", "FLOOR", "ROOMS", "MICROWAVE", "SINK", "KITCHEN", "FRIDGE" },
                    new[] { "KITCHEN", "FRIDGE", "MICROWAVE", "SINK" },
                    new[] { "BEDROOM", "BED" },
                    new[] { "ROOMS", "KITCHEN", "BEDROOM" },
                    new[] { "LOCAL-GLOBALS", "FLOOR", "CEILING" }));
        }

        [TestMethod]
        public void TestHouse_Objects_RoomsFirst()
        {
            AssertGlobals(
                "<ORDER-OBJECTS? ROOMS-FIRST>",
                "<OBJECT FRIDGE (IN KITCHEN)>",
                "<OBJECT SINK (IN KITCHEN)>",
                "<OBJECT MICROWAVE (IN KITCHEN)>",
                "<ROOM KITCHEN (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT FLOOR (IN LOCAL-GLOBALS)>",
                "<ROOM BEDROOM (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT BED (IN BEDROOM)>",
                "<OBJECT ROOMS>",
                "<OBJECT LOCAL-GLOBALS>",
                "<OBJECT CEILING (IN LOCAL-GLOBALS)>")
                .Implies(TreeImplications(
                    new[] { "KITCHEN", "BEDROOM", "FRIDGE", "SINK", "MICROWAVE", "ROOMS", "FLOOR", "CEILING", "LOCAL-GLOBALS", "BED" },
                    new[] { "KITCHEN", "FRIDGE", "MICROWAVE", "SINK" },
                    new[] { "BEDROOM", "BED" },
                    new[] { "ROOMS", "KITCHEN", "BEDROOM" },
                    new[] { "LOCAL-GLOBALS", "FLOOR", "CEILING" }));
        }

        [TestMethod]
        public void TestHouse_Objects_RoomsAndLgsFirst()
        {
            AssertGlobals(
                "<ORDER-OBJECTS? ROOMS-AND-LGS-FIRST>",
                "<OBJECT FRIDGE (IN KITCHEN)>",
                "<OBJECT SINK (IN KITCHEN)>",
                "<OBJECT MICROWAVE (IN KITCHEN)>",
                "<ROOM KITCHEN (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT FLOOR (IN LOCAL-GLOBALS)>",
                "<ROOM BEDROOM (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT BED (IN BEDROOM)>",
                "<OBJECT ROOMS>",
                "<OBJECT LOCAL-GLOBALS>",
                "<OBJECT CEILING (IN LOCAL-GLOBALS)>")
                .Implies(TreeImplications(
                    new[] { "KITCHEN", "FLOOR", "CEILING", "BEDROOM", "FRIDGE", "SINK", "MICROWAVE", "ROOMS", "LOCAL-GLOBALS", "BED" },
                    new[] { "KITCHEN", "FRIDGE", "MICROWAVE", "SINK" },
                    new[] { "BEDROOM", "BED" },
                    new[] { "ROOMS", "KITCHEN", "BEDROOM" },
                    new[] { "LOCAL-GLOBALS", "FLOOR", "CEILING" }));
        }

        [TestMethod]
        public void TestHouse_Objects_RoomsLast()
        {
            AssertGlobals(
                "<ORDER-OBJECTS? ROOMS-LAST>",
                "<OBJECT FRIDGE (IN KITCHEN)>",
                "<OBJECT SINK (IN KITCHEN)>",
                "<OBJECT MICROWAVE (IN KITCHEN)>",
                "<ROOM KITCHEN (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT FLOOR (IN LOCAL-GLOBALS)>",
                "<ROOM BEDROOM (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT BED (IN BEDROOM)>",
                "<OBJECT ROOMS>",
                "<OBJECT LOCAL-GLOBALS>",
                "<OBJECT CEILING (IN LOCAL-GLOBALS)>")
                .Implies(TreeImplications(
                    new[] { "FRIDGE", "SINK", "MICROWAVE", "ROOMS", "FLOOR", "CEILING", "LOCAL-GLOBALS", "BED", "KITCHEN", "BEDROOM" },
                    new[] { "KITCHEN", "FRIDGE", "MICROWAVE", "SINK" },
                    new[] { "BEDROOM", "BED" },
                    new[] { "ROOMS", "KITCHEN", "BEDROOM" },
                    new[] { "LOCAL-GLOBALS", "FLOOR", "CEILING" }));
        }

        [TestMethod]
        public void TestHouse_Objects_Defined()
        {
            AssertGlobals(
                "<ORDER-OBJECTS? DEFINED>",
                "<OBJECT FRIDGE (IN KITCHEN)>",
                "<OBJECT SINK (IN KITCHEN)>",
                "<OBJECT MICROWAVE (IN KITCHEN)>",
                "<ROOM KITCHEN (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT FLOOR (IN LOCAL-GLOBALS)>",
                "<ROOM BEDROOM (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT BED (IN BEDROOM)>",
                "<OBJECT ROOMS>",
                "<OBJECT LOCAL-GLOBALS>",
                "<OBJECT CEILING (IN LOCAL-GLOBALS)>")
                .Implies(TreeImplications(
                    new[] { "FRIDGE", "SINK", "MICROWAVE", "KITCHEN", "FLOOR", "BEDROOM", "BED", "ROOMS", "LOCAL-GLOBALS", "CEILING" },
                    new[] { "KITCHEN", "FRIDGE", "MICROWAVE", "SINK" },
                    new[] { "BEDROOM", "BED" },
                    new[] { "ROOMS", "KITCHEN", "BEDROOM" },
                    new[] { "LOCAL-GLOBALS", "FLOOR", "CEILING" }));
        }

        // TODO: tests for other <ORDER-OBJECTS? ...>


        /* <ORDER-TREE? REVERSE-DEFINED>:
         * 
         * The object tree is traversed in reverse definition order (with no exception for
         * the first defined child).
         */

        [TestMethod]
        public void TestContents_Tree_ReverseDefined()
        {
            AssertGlobals(
                "<ORDER-TREE? REVERSE-DEFINED>",
                "<OBJECT RAINBOW>",
                "<OBJECT RED (IN RAINBOW)>",
                "<OBJECT YELLOW (IN RAINBOW)>",
                "<OBJECT GREEN (IN RAINBOW)>",
                "<OBJECT BLUE (IN RAINBOW)>")
                .Implies(TreeImplications(
                    new[] { "BLUE", "GREEN", "YELLOW", "RED", "RAINBOW" },
                    new[] { "RAINBOW", "BLUE", "GREEN", "YELLOW", "RED" }));
        }

        [TestMethod]
        public void TestHouse_Tree_ReverseDefined()
        {
            AssertGlobals(
                "<ORDER-TREE? REVERSE-DEFINED>",
                "<OBJECT FRIDGE (IN KITCHEN)>",
                "<OBJECT SINK (IN KITCHEN)>",
                "<OBJECT MICROWAVE (IN KITCHEN)>",
                "<ROOM KITCHEN (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<ROOM BEDROOM (IN ROOMS) (GLOBAL FLOOR CEILING)>",
                "<OBJECT BED (IN BEDROOM)>",
                "<OBJECT ROOMS>",
                "<OBJECT LOCAL-GLOBALS>",
                "<OBJECT FLOOR (IN LOCAL-GLOBALS)>",
                "<OBJECT CEILING (IN LOCAL-GLOBALS)>")
                .Implies(TreeImplications(
                    new[] { "LOCAL-GLOBALS", "BED", "BEDROOM", "CEILING", "FLOOR", "ROOMS", "MICROWAVE", "SINK", "KITCHEN", "FRIDGE" },
                    new[] { "KITCHEN", "MICROWAVE", "SINK", "FRIDGE" },
                    new[] { "BEDROOM", "BED" },
                    new[] { "ROOMS", "BEDROOM", "KITCHEN" },
                    new[] { "LOCAL-GLOBALS", "CEILING", "FLOOR" }));
        }

        #endregion

        #region Attribute Numbering

        [TestMethod]
        public void Bits_Mentioned_In_FIND_Must_Be_Nonzero()
        {
            AssertGlobals(
                "<OBJECT FOO (FLAGS F1BIT F2BIT F3BIT F4BIT F5BIT F6BIT F7BIT F8BIT " +
                                   "F9BIT F10BIT F11BIT F12BIT F13BIT F14BIT F15BIT F16BIT " +
                                   "F17BIT F18BIT F19BIT F20BIT F21BIT F22BIT F23BIT F24BIT " +
                                   "F25BIT F26BIT F27BIT F28BIT F29BIT F30BIT F31BIT F32BIT)>",
                "<SYNTAX BAR OBJECT (FIND F1BIT) WITH OBJECT (FIND F2BIT) = V-BAR>",
                "<SYNTAX BAZ OBJECT (FIND F31BIT) WITH OBJECT (FIND F32BIT) = V-BAZ>",
                "<ROUTINE V-BAR () <>>",
                "<ROUTINE V-BAZ () <>>")
                .Implies(
                    "<NOT <0? ,F1BIT>>",
                    "<NOT <0? ,F2BIT>>",
                    "<NOT <0? ,F31BIT>>",
                    "<NOT <0? ,F32BIT>>");
        }

        [TestMethod]
        public void Bit_Synonym_Should_Work_In_FLAGS()
        {
            AssertRoutine("", "<AND <==? ,MAINBIT ,ALIASBIT> <FSET? ,FOO ,MAINBIT> <FSET? ,BAR ,ALIASBIT>>")
                .WithGlobal("<BIT-SYNONYM MAINBIT ALIASBIT>")
                .WithGlobal("<OBJECT FOO (FLAGS MAINBIT)>")
                .WithGlobal("<OBJECT BAR (FLAGS ALIASBIT)>")
                .GivesNumber("1");
        }

        [TestMethod]
        public void Bit_Synonym_Should_Not_Be_Clobbered_By_FIND()
        {
            AssertRoutine("", "<==? ,MAINBIT ,ALIASBIT>")
                .WithGlobal("<BIT-SYNONYM MAINBIT ALIASBIT>")
                .WithGlobal("<OBJECT FOO (FLAGS MAINBIT)>")
                .WithGlobal("<OBJECT BAR (FLAGS ALIASBIT)>")
                .WithGlobal("<SYNTAX FOO OBJECT (FIND ALIASBIT) = V-FOO>")
                .WithGlobal("<ROUTINE V-FOO () <>>")
                .GivesNumber("1");
        }

        [TestMethod]
        public void Bit_Synonym_Should_Work_Even_If_Original_Is_Never_Set()
        {
            AssertGlobals(
                "<BIT-SYNONYM MAINBIT ALIASBIT>",
                "<OBJECT FOO (FLAGS ALIASBIT)>")
                .Compiles();
        }

        [TestMethod]
        public void Too_Many_Bits_Should_Spoil_The_Build()
        {
            var tooManyBits = new StringBuilder();

            // V3 limit: 32 flags
            for (int i = 0; i < 33; i++)
            {
                tooManyBits.AppendFormat(" TESTBIT{0}", i);
            }

            AssertGlobals(
                string.Format("<OBJECT FOO (FLAGS {0})>", tooManyBits))
                .InV3()
                .DoesNotCompile();

            // V4+ limit: 48 flags
            for (int i = 32; i < 49; i++)
            {
                tooManyBits.AppendFormat(" TESTBIT{0}", i);
            }

            AssertGlobals(
                string.Format("<OBJECT FOO (FLAGS {0})>", tooManyBits))
                .InV4()
                .DoesNotCompile();
        }

        #endregion

        #region PROPDEF/PROPSPEC

        [TestMethod]
        public void PROPDEF_Basic_Pattern_Should_Work()
        {
            AssertGlobals(
                "<PROPDEF HEIGHT <> " +
                " (HEIGHT FEET:FIX FOOT INCHES:FIX = 2 <WORD .FEET> <BYTE .INCHES>)" +
                " (HEIGHT FEET:FIX FT INCHES:FIX = 2 <WORD .FEET> <BYTE .INCHES>)>",
                "<OBJECT GIANT (HEIGHT 10 FT 8)>")
                .Implies(
                    "<=? <GET <GETPT ,GIANT ,P?HEIGHT> 0> 10>",
                    "<=? <GETB <GETPT ,GIANT ,P?HEIGHT> 2> 8>");
        }

        [TestMethod]
        public void PROPDEF_OPT_Should_Work()
        {
            AssertGlobals(
                "<PROPDEF HEIGHT <> " +
                " (HEIGHT FEET:FIX FT \"OPT\" INCHES:FIX = <WORD .FEET> <BYTE .INCHES>)>",
                "<OBJECT GIANT1 (HEIGHT 100 FT)>",
                "<OBJECT GIANT2 (HEIGHT 50 FT 11)>")
                .Implies(
                    "<=? <PTSIZE <GETPT ,GIANT1 ,P?HEIGHT>> 3>",
                    "<=? <GET <GETPT ,GIANT1 ,P?HEIGHT> 0> 100>",
                    "<=? <GETB <GETPT ,GIANT1 ,P?HEIGHT> 2> 0>",
                    "<=? <PTSIZE <GETPT ,GIANT2 ,P?HEIGHT>> 3>",
                    "<=? <GET <GETPT ,GIANT2 ,P?HEIGHT> 0> 50>",
                    "<=? <GETB <GETPT ,GIANT2 ,P?HEIGHT> 2> 11>");
        }

        [TestMethod]
        public void PROPDEF_MANY_Should_Work()
        {
            AssertGlobals(
                "<PROPDEF TRANSLATE <> " +
                " (TRANSLATE \"MANY\" A:ATOM N:FIX = \"MANY\" <VOC .A BUZZ> <WORD .N>)>",
                "<OBJECT NUMBERS (TRANSLATE ONE 1 TWO 2)>")
                .Implies(
                    "<=? <PTSIZE <GETPT ,NUMBERS ,P?TRANSLATE>> 8>",
                    "<=? <GET <GETPT ,NUMBERS ,P?TRANSLATE> 0> ,W?ONE>",
                    "<=? <GET <GETPT ,NUMBERS ,P?TRANSLATE> 1> 1>",
                    "<=? <GET <GETPT ,NUMBERS ,P?TRANSLATE> 2> ,W?TWO>",
                    "<=? <GET <GETPT ,NUMBERS ,P?TRANSLATE> 3> 2>");
        }

        [TestMethod]
        public void PROPDEF_Constants_Should_Work()
        {
            AssertGlobals(
                "<PROPDEF HEIGHT <> " +
                " (HEIGHT FEET:FIX FT INCHES:FIX = (HEIGHTSIZE 3) (H-FEET <WORD .FEET>) (H-INCHES <BYTE .INCHES>))>")
                .Implies(
                    "<=? ,HEIGHTSIZE 3>",
                    "<=? ,H-FEET 0>",
                    "<=? ,H-INCHES 2>");
        }

        [TestMethod]
        public void PROPDEF_For_DIRECTIONS_Should_Be_Used_For_All_Directions()
        {
            AssertGlobals(
                "<PROPDEF DIRECTIONS <> " +
                " (DIR GOES TO R:ROOM = (MY-UEXIT 3) <WORD 0> (MY-REXIT <ROOM .R>))>",
                "<DIRECTIONS NORTH SOUTH>",
                "<OBJECT HOUSE (SOUTH GOES TO WOODS)>",
                "<OBJECT WOODS (NORTH GOES TO HOUSE)>")
                .Implies(
                    "<=? <PTSIZE <GETPT ,HOUSE ,P?SOUTH>> ,MY-UEXIT>",
                    "<=? <GETB <GETPT ,HOUSE ,P?SOUTH> ,MY-REXIT> ,WOODS>");
        }

        [TestMethod]
        public void Clearing_PROPSPEC_For_DIRECTIONS_Should_Override_Default_Patterns()
        {
            AssertGlobals(
                "<PUTPROP DIRECTIONS PROPSPEC>",
                "<DIRECTIONS NORTH SOUTH>",
                "<OBJECT HOUSE (SOUTH TO WOODS)>",
                "<OBJECT WOODS (NORTH TO HOUSE)>")
                .DoesNotCompile();
        }

        [TestMethod]
        public void PROPDEF_For_DIRECTIONS_Can_Be_Used_For_Implicit_Directions()
        {
            AssertGlobals(
                "<PROPDEF DIRECTIONS <> " +
                " (DIR GOES TO R:ROOM = (MY-UEXIT 3) <WORD 0> (MY-REXIT <ROOM .R>))>",
                "<DIRECTIONS NORTH SOUTH>",
                "<OBJECT HOUSE (EAST GOES TO WOODS)>",
                "<OBJECT WOODS (WEST GOES TO HOUSE)>")
                .Implies(
                    "<=? <PTSIZE <GETPT ,HOUSE ,P?EAST>> ,MY-UEXIT>",
                    "<=? <GETB <GETPT ,HOUSE ,P?EAST> ,MY-REXIT> ,WOODS>",
                    "<BAND <GETB ,W?EAST 4> ,PS?DIRECTION>");
        }

        [TestMethod]
        public void Vocab_Created_By_PROPDEF_Should_Work_Correctly()
        {
            AssertGlobals(
                "<PROPDEF FOO <> (FOO A:ATOM = <VOC .A PREP>)>",
                "<OBJECT BAR (FOO FOO)>")
                .Implies(
                    "<=? <GETP ,BAR ,P?FOO> ,W?FOO>");
        }

        [TestMethod]
        public void Vocab_Created_By_PROPSPEC_Should_Work_Correctly()
        {
            AssertGlobals(
                "<PUTPROP FOO PROPSPEC FOO-PROP>",
                "<DEFINE FOO-PROP (L) (<> <EVAL <CHTYPE (TABLE <VOC \"FOO\" PREP>) FORM>>)>",
                "<OBJECT BAR (FOO FOO)>")
                .Implies(
                    "<=? <GET <GETP ,BAR ,P?FOO> 0> ,W?FOO>");
        }

        [TestMethod]
        public void Routines_Created_By_PROPSPEC_Should_Work_Correctly()
        {
            AssertGlobals(
                "<PUTPROP FOO PROPSPEC FOO-PROP>",
                "<DEFINE FOO-PROP (L) <ROUTINE PROP-ROUTINE () 123> (<> PROP-ROUTINE)>",
                "<OBJECT BAR (FOO FOO)>")
                .Implies(
                    "<=? <APPLY <GETP ,BAR ,P?FOO>> 123>");
        }

        #endregion

        [TestMethod]
        public void Non_Constants_As_Property_Values_Should_Be_Rejected()
        {
            AssertGlobals(
                "<GLOBAL FOO 123>",
                "<OBJECT BAR (BAZ FOO)>")
                .DoesNotCompile();
        }

        [TestMethod]
        public void Non_Constants_In_Property_Initializers_Should_Be_Rejected()
        {
            AssertGlobals(
                "<GLOBAL FOO 123>",
                "<OBJECT BAR (BAZ 4 5 FOO)>")
                .DoesNotCompile();
        }

        [TestMethod]
        public void Nonexistent_Object_In_Direction_Property_Should_Be_Rejected()
        {
            AssertGlobals(
                "<DIRECTIONS NORTH>",
                "<OBJECT FOO (NORTH TO BAR)>")
                .DoesNotCompile();
        }

        [TestMethod]
        public void Direction_Synonyms_Should_Work_Identically()
        {
            AssertGlobals(
                "<DIRECTIONS SOUTHWEST>",
                "<SYNONYM SOUTHWEST SW>",
                "<OBJECT FOO (SW TO FOO)>")
                .InV3()
                .Implies(
                    "<=? ,P?SOUTHWEST ,P?SW>",
                    "<=? <GETB ,W?SW 5> ,P?SOUTHWEST>",
                    "<=? <GETB ,W?SOUTHWEST 5> ,P?SOUTHWEST>");
        }

        [TestMethod]
        public void Direction_Properties_Should_Not_Be_Merged_With_Words()
        {
            AssertGlobals(
                "<DIRECTIONS NORTHNORTHEAST NORTHNORTHWEST>",
                "<OBJECT FOO (NORTHNORTHEAST TO FOO) (NORTHNORTHWEST TO BAR)>",
                "<OBJECT BAR>")
                .InV3()
                .Implies(
                    "<=? ,W?NORTHNORTHEAST ,W?NORTHNORTHWEST>",
                    "<N=? ,P?NORTHNORTHEAST ,P?NORTHNORTHWEST>",
                    "<=? <GETP ,FOO ,P?NORTHNORTHEAST> ,FOO>",
                    "<=? <GETP ,FOO ,P?NORTHNORTHWEST> ,BAR>");
        }

        [TestMethod]
        public void ROOM_In_PROPDEF_Should_Be_One_Byte_When_ORDER_OBJECTS_Is_ROOMS_FIRST()
        {
            AssertGlobals(
                "<ORDER-OBJECTS? ROOMS-FIRST>",
                "<DIRECTIONS NORTH>",
                "<PROPDEF DIRECTIONS <> (DIR TO R:ROOM = (UEXIT 1) (REXIT <ROOM .R>))>",
                "<OBJECT FOO (NORTH TO BAR)>",
                "<OBJECT BAR>")
                .InV5()
                .Implies(
                    "<=? <PTSIZE <GETPT ,FOO ,P?NORTH>> 1>");
        }

        [TestMethod]
        public void Duplicate_Property_Definitions_Should_Not_Be_Allowed()
        {
            // user-defined property
            AssertGlobals(
                "<OBJECT FOO (MYPROP 1) (MYPROP 2)>")
                .DoesNotCompile();

            // standard pseudo-properties
            AssertGlobals(
                "<OBJECT FOO (DESC \"foo\") (DESC \"bar\")>")
                .DoesNotCompile();

            AssertGlobals(
                "<OBJECT ROOM1>",
                "<OBJECT ROOM2>",
                "<OBJECT FOO (IN ROOM1) (LOC ROOM2)>")
                .DoesNotCompile();
        }

        [TestMethod]
        public void IN_Pseudo_Property_Should_Not_Conflict_With_IN_String_NEXIT()
        {
            AssertGlobals(
                "<DIRECTIONS IN>",
                "<OBJECT ROOMS>",
                "<OBJECT FOO (IN ROOMS) (IN \"You can't go in.\")>")
                .Compiles();

            // even if IN isn't defined as a direction!
            AssertGlobals(
                "<OBJECT ROOMS>",
                "<OBJECT FOO (IN ROOMS) (IN \"You can't go in.\")>")
                .Compiles();
        }

        [TestMethod]
        public void Multiple_FLAGS_Definitions_Should_Combine()
        {
            AssertGlobals(
                "<OBJECT FOO (FLAGS FOOBIT) (FLAGS BARBIT)>")
                .Implies(
                    "<FSET? ,FOO ,FOOBIT>",
                    "<FSET? ,FOO ,BARBIT>");
        }
    }
}
