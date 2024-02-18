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

using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zilf.Tests.Integration
{
    [TestClass, TestCategory("Compiler"), TestCategory("Objects")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Test methods are only called once")]
    public class ObjectTests : IntegrationTestClass
    {
        #region Object Numbering & Tree Ordering

        /* Default ordering:
         * 
         * Objects are numbered in reverse definition-or-mention order.
         * 
         * The object tree is traversed in reverse definition order (ignoring mere mentions),
         * except that the first child defined is the first child traversed (not the last).
         */

        [TestMethod]
        public async Task TestContents_DefaultOrder()
        {
            await AssertGlobals(
                "<OBJECT RAINBOW>",
                "<OBJECT RED (IN RAINBOW)>",
                "<OBJECT YELLOW (IN RAINBOW)>",
                "<OBJECT GREEN (IN RAINBOW)>",
                "<OBJECT BLUE (IN RAINBOW)>")
                .ImpliesAsync(TreeImplications(
                    ["BLUE", "GREEN", "YELLOW", "RED", "RAINBOW"],
                    new[] { "RAINBOW", "RED", "BLUE", "GREEN", "YELLOW" }));
        }

        [TestMethod]
        public async Task TestHouse_DefaultOrder()
        {
            await AssertGlobals(
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
                .ImpliesAsync(TreeImplications(
                    ["BED", "BEDROOM", "LOCAL-GLOBALS", "CEILING", "FLOOR", "ROOMS", "MICROWAVE", "SINK", "KITCHEN", "FRIDGE"],
                    ["KITCHEN", "FRIDGE", "MICROWAVE", "SINK"],
                    ["BEDROOM", "BED"],
                    ["ROOMS", "KITCHEN", "BEDROOM"],
                    ["LOCAL-GLOBALS", "FLOOR", "CEILING"]));
        }

        [TestMethod]
        public async Task TestHouse_Objects_RoomsFirst()
        {
            await AssertGlobals(
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
                .ImpliesAsync(TreeImplications(
                    ["KITCHEN", "BEDROOM", "FRIDGE", "SINK", "MICROWAVE", "ROOMS", "FLOOR", "CEILING", "LOCAL-GLOBALS", "BED"],
                    ["KITCHEN", "FRIDGE", "MICROWAVE", "SINK"],
                    ["BEDROOM", "BED"],
                    ["ROOMS", "KITCHEN", "BEDROOM"],
                    ["LOCAL-GLOBALS", "FLOOR", "CEILING"]));
        }

        [TestMethod]
        public async Task TestHouse_Objects_RoomsAndLgsFirst()
        {
            await AssertGlobals(
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
                .ImpliesAsync(TreeImplications(
                    ["KITCHEN", "FLOOR", "CEILING", "BEDROOM", "FRIDGE", "SINK", "MICROWAVE", "ROOMS", "LOCAL-GLOBALS", "BED"],
                    ["KITCHEN", "FRIDGE", "MICROWAVE", "SINK"],
                    ["BEDROOM", "BED"],
                    ["ROOMS", "KITCHEN", "BEDROOM"],
                    ["LOCAL-GLOBALS", "FLOOR", "CEILING"]));
        }

        [TestMethod]
        public async Task TestHouse_Objects_RoomsLast()
        {
            await AssertGlobals(
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
                .ImpliesAsync(TreeImplications(
                    ["FRIDGE", "SINK", "MICROWAVE", "ROOMS", "FLOOR", "CEILING", "LOCAL-GLOBALS", "BED", "KITCHEN", "BEDROOM"],
                    ["KITCHEN", "FRIDGE", "MICROWAVE", "SINK"],
                    ["BEDROOM", "BED"],
                    ["ROOMS", "KITCHEN", "BEDROOM"],
                    ["LOCAL-GLOBALS", "FLOOR", "CEILING"]));
        }

        [TestMethod]
        public async Task TestHouse_Objects_Defined()
        {
            await AssertGlobals(
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
                .ImpliesAsync(TreeImplications(
                    ["FRIDGE", "SINK", "MICROWAVE", "KITCHEN", "FLOOR", "BEDROOM", "BED", "ROOMS", "LOCAL-GLOBALS", "CEILING"],
                    ["KITCHEN", "FRIDGE", "MICROWAVE", "SINK"],
                    ["BEDROOM", "BED"],
                    ["ROOMS", "KITCHEN", "BEDROOM"],
                    ["LOCAL-GLOBALS", "FLOOR", "CEILING"]));
        }

        // TODO: tests for other <ORDER-OBJECTS? ...>


        /* <ORDER-TREE? REVERSE-DEFINED>:
         * 
         * The object tree is traversed in reverse definition order (with no exception for
         * the first defined child).
         */

        [TestMethod]
        public async Task TestContents_Tree_ReverseDefined()
        {
            await AssertGlobals(
                "<ORDER-TREE? REVERSE-DEFINED>",
                "<OBJECT RAINBOW>",
                "<OBJECT RED (IN RAINBOW)>",
                "<OBJECT YELLOW (IN RAINBOW)>",
                "<OBJECT GREEN (IN RAINBOW)>",
                "<OBJECT BLUE (IN RAINBOW)>")
                .ImpliesAsync(TreeImplications(
                    ["BLUE", "GREEN", "YELLOW", "RED", "RAINBOW"],
                    new[] { "RAINBOW", "BLUE", "GREEN", "YELLOW", "RED" }));
        }

        [TestMethod]
        public async Task TestHouse_Tree_ReverseDefined()
        {
            await AssertGlobals(
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
                .ImpliesAsync(TreeImplications(
                    ["LOCAL-GLOBALS", "BED", "BEDROOM", "CEILING", "FLOOR", "ROOMS", "MICROWAVE", "SINK", "KITCHEN", "FRIDGE"],
                    ["KITCHEN", "MICROWAVE", "SINK", "FRIDGE"],
                    ["BEDROOM", "BED"],
                    ["ROOMS", "BEDROOM", "KITCHEN"],
                    ["LOCAL-GLOBALS", "CEILING", "FLOOR"]));
        }

        #endregion

        #region Attribute Numbering

        [TestMethod]
        public async Task Bits_Mentioned_In_FIND_Must_Be_Nonzero()
        {
            await AssertGlobals(
                "<OBJECT FOO (FLAGS F1BIT F2BIT F3BIT F4BIT F5BIT F6BIT F7BIT F8BIT " +
                                   "F9BIT F10BIT F11BIT F12BIT F13BIT F14BIT F15BIT F16BIT " +
                                   "F17BIT F18BIT F19BIT F20BIT F21BIT F22BIT F23BIT F24BIT " +
                                   "F25BIT F26BIT F27BIT F28BIT F29BIT F30BIT F31BIT F32BIT)>",
                "<SYNTAX BAR OBJECT (FIND F1BIT) WITH OBJECT (FIND F2BIT) = V-BAR>",
                "<SYNTAX BAZ OBJECT (FIND F31BIT) WITH OBJECT (FIND F32BIT) = V-BAZ>",
                "<ROUTINE V-BAR () <>>",
                "<ROUTINE V-BAZ () <>>")
                .ImpliesAsync(
                    "<NOT <0? ,F1BIT>>",
                    "<NOT <0? ,F2BIT>>",
                    "<NOT <0? ,F31BIT>>",
                    "<NOT <0? ,F32BIT>>");
        }

        [TestMethod]
        public async Task Bit_Synonym_Should_Work_In_FLAGS()
        {
            await AssertRoutine("", "<AND <==? ,MAINBIT ,ALIASBIT> <FSET? ,FOO ,MAINBIT> <FSET? ,BAR ,ALIASBIT>>")
                .WithGlobal("<BIT-SYNONYM MAINBIT ALIASBIT>")
                .WithGlobal("<OBJECT FOO (FLAGS MAINBIT)>")
                .WithGlobal("<OBJECT BAR (FLAGS ALIASBIT)>")
                .GivesNumberAsync("1");
        }

        [TestMethod]
        public async Task Bit_Synonym_Should_Not_Be_Clobbered_By_FIND()
        {
            await AssertRoutine("", "<==? ,MAINBIT ,ALIASBIT>")
                .WithGlobal("<BIT-SYNONYM MAINBIT ALIASBIT>")
                .WithGlobal("<OBJECT FOO (FLAGS MAINBIT)>")
                .WithGlobal("<OBJECT BAR (FLAGS ALIASBIT)>")
                .WithGlobal("<SYNTAX FOO OBJECT (FIND ALIASBIT) = V-FOO>")
                .WithGlobal("<ROUTINE V-FOO () <>>")
                .GivesNumberAsync("1");
        }

        [TestMethod]
        public async Task Bit_Synonym_Should_Work_Even_If_Original_Is_Never_Set()
        {
            await AssertGlobals(
                "<BIT-SYNONYM MAINBIT ALIASBIT>",
                "<OBJECT FOO (FLAGS ALIASBIT)>")
                .CompilesAsync();
        }

        [TestMethod]
        public async Task Too_Many_Bits_Should_Spoil_The_Build()
        {
            var tooManyBits = new StringBuilder();

            // V3 limit: 32 flags
            for (int i = 0; i < 33; i++)
            {
                tooManyBits.AppendFormat(" TESTBIT{0}", i);
            }

            await AssertGlobals(
                $"<OBJECT FOO (FLAGS {tooManyBits})>")
                .InV3()
                .DoesNotCompileAsync();

            // V4+ limit: 48 flags
            for (int i = 32; i < 49; i++)
            {
                tooManyBits.AppendFormat(" TESTBIT{0}", i);
            }

            await AssertGlobals(
                $"<OBJECT FOO (FLAGS {tooManyBits})>")
                .InV4()
                .DoesNotCompileAsync();
        }

        #endregion

        #region PROPDEF/PROPSPEC

        [TestMethod]
        public async Task PROPDEF_Basic_Pattern_Should_Work()
        {
            await AssertGlobals(
                "<PROPDEF HEIGHT <> " +
                " (HEIGHT FEET:FIX FOOT INCHES:FIX = 2 <WORD .FEET> <BYTE .INCHES>)" +
                " (HEIGHT FEET:FIX FT INCHES:FIX = 2 <WORD .FEET> <BYTE .INCHES>)>",
                "<OBJECT GIANT (HEIGHT 10 FT 8)>")
                .ImpliesAsync(
                    "<=? <GET <GETPT ,GIANT ,P?HEIGHT> 0> 10>",
                    "<=? <GETB <GETPT ,GIANT ,P?HEIGHT> 2> 8>");
        }

        [TestMethod]
        public async Task PROPDEF_OPT_Should_Work()
        {
            await AssertGlobals(
                "<PROPDEF HEIGHT <> " +
                " (HEIGHT FEET:FIX FT \"OPT\" INCHES:FIX = <WORD .FEET> <BYTE .INCHES>)>",
                "<OBJECT GIANT1 (HEIGHT 100 FT)>",
                "<OBJECT GIANT2 (HEIGHT 50 FT 11)>")
                .ImpliesAsync(
                    "<=? <PTSIZE <GETPT ,GIANT1 ,P?HEIGHT>> 3>",
                    "<=? <GET <GETPT ,GIANT1 ,P?HEIGHT> 0> 100>",
                    "<=? <GETB <GETPT ,GIANT1 ,P?HEIGHT> 2> 0>",
                    "<=? <PTSIZE <GETPT ,GIANT2 ,P?HEIGHT>> 3>",
                    "<=? <GET <GETPT ,GIANT2 ,P?HEIGHT> 0> 50>",
                    "<=? <GETB <GETPT ,GIANT2 ,P?HEIGHT> 2> 11>");
        }

        [TestMethod]
        public async Task PROPDEF_MANY_Should_Work()
        {
            await AssertGlobals(
                "<PROPDEF TRANSLATE <> " +
                " (TRANSLATE \"MANY\" A:ATOM N:FIX = \"MANY\" <VOC .A BUZZ> <WORD .N>)>",
                "<OBJECT NUMBERS (TRANSLATE ONE 1 TWO 2)>")
                .ImpliesAsync(
                    "<=? <PTSIZE <GETPT ,NUMBERS ,P?TRANSLATE>> 8>",
                    "<=? <GET <GETPT ,NUMBERS ,P?TRANSLATE> 0> ,W?ONE>",
                    "<=? <GET <GETPT ,NUMBERS ,P?TRANSLATE> 1> 1>",
                    "<=? <GET <GETPT ,NUMBERS ,P?TRANSLATE> 2> ,W?TWO>",
                    "<=? <GET <GETPT ,NUMBERS ,P?TRANSLATE> 3> 2>");
        }

        [TestMethod]
        public async Task PROPDEF_Constants_Should_Work()
        {
            await AssertGlobals(
                "<PROPDEF HEIGHT <> " +
                " (HEIGHT FEET:FIX FT INCHES:FIX = (HEIGHTSIZE 3) (H-FEET <WORD .FEET>) (H-INCHES <BYTE .INCHES>))>")
                .ImpliesAsync(
                    "<=? ,HEIGHTSIZE 3>",
                    "<=? ,H-FEET 0>",
                    "<=? ,H-INCHES 2>");
        }

        [TestMethod]
        public async Task PROPDEF_With_Empty_FORM_For_Length_Should_Work()
        {
            await AssertGlobals(
                    "<PROPDEF HEIGHT <> " +
                    " (HEIGHT FEET:FIX FT INCHES:FIX = <> (H-FEET <WORD .FEET>) (H-INCHES <BYTE .INCHES>))>")
                .CompilesAsync();
        }


        [TestMethod]
        public async Task PROPDEF_For_DIRECTIONS_Should_Be_Used_For_All_Directions()
        {
            await AssertGlobals(
                "<PROPDEF DIRECTIONS <> " +
                " (DIR GOES TO R:ROOM = (MY-UEXIT 3) <WORD 0> (MY-REXIT <ROOM .R>))>",
                "<DIRECTIONS NORTH SOUTH>",
                "<OBJECT HOUSE (SOUTH GOES TO WOODS)>",
                "<OBJECT WOODS (NORTH GOES TO HOUSE)>")
                .ImpliesAsync(
                    "<=? <PTSIZE <GETPT ,HOUSE ,P?SOUTH>> ,MY-UEXIT>",
                    "<=? <GETB <GETPT ,HOUSE ,P?SOUTH> ,MY-REXIT> ,WOODS>");
        }

        [TestMethod]
        public async Task Clearing_PROPSPEC_For_DIRECTIONS_Should_Override_Default_Patterns()
        {
            await AssertGlobals(
                "<PUTPROP DIRECTIONS PROPSPEC>",
                "<DIRECTIONS NORTH SOUTH>",
                "<OBJECT HOUSE (SOUTH TO WOODS)>",
                "<OBJECT WOODS (NORTH TO HOUSE)>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task PROPDEF_For_DIRECTIONS_Can_Be_Used_For_Implicit_Directions()
        {
            await AssertGlobals(
                "<PROPDEF DIRECTIONS <> " +
                " (DIR GOES TO R:ROOM = (MY-UEXIT 3) <WORD 0> (MY-REXIT <ROOM .R>))>",
                "<DIRECTIONS NORTH SOUTH>",
                "<OBJECT HOUSE (EAST GOES TO WOODS)>",
                "<OBJECT WOODS (WEST GOES TO HOUSE)>")
                .ImpliesAsync(
                    "<=? <PTSIZE <GETPT ,HOUSE ,P?EAST>> ,MY-UEXIT>",
                    "<=? <GETB <GETPT ,HOUSE ,P?EAST> ,MY-REXIT> ,WOODS>",
                    "<BAND <GETB ,W?EAST 4> ,PS?DIRECTION>");
        }

        [TestMethod]
        public async Task PROPDEF_For_DIRECTIONS_Should_Not_Create_A_DIRECTIONS_Property()
        {
            await AssertGlobals(
                "<PROPDEF DIRECTIONS <> " +
                " (DIR GOES TO R:ROOM = (MY-UEXIT 3) <WORD 0> (MY-REXIT <ROOM .R>))>",
                "<DIRECTIONS NORTH SOUTH>",
                "<OBJECT HOUSE (SOUTH GOES TO WOODS)>",
                "<OBJECT WOODS (NORTH GOES TO HOUSE)>",
                "<ROUTINE FOO () ,P?DIRECTIONS>")
                .DoesNotCompileAsync();

            await AssertGlobals(
                "<PROPDEF DIRECTIONS <> " +
                " (DIR GOES TO R:ROOM = (MY-UEXIT 3) <WORD 0> (MY-REXIT <ROOM .R>))>",
                "<DIRECTIONS NORTH SOUTH>",
                "<OBJECT HOUSE (SOUTH GOES TO WOODS)>",
                "<OBJECT WOODS (NORTH GOES TO HOUSE)>")
                .GeneratesCodeNotMatchingAsync(@"P\?DIRECTIONS");
        }

        [TestMethod]
        public async Task Vocab_Created_By_PROPDEF_Should_Work_Correctly()
        {
            await AssertGlobals(
                "<PROPDEF FOO <> (FOO A:ATOM = <VOC .A PREP>)>",
                "<OBJECT BAR (FOO FOO)>")
                .ImpliesAsync(
                    "<=? <GETP ,BAR ,P?FOO> ,W?FOO>");
        }

        [TestMethod]
        public async Task Vocab_Created_By_PROPSPEC_Should_Work_Correctly()
        {
            await AssertGlobals(
                "<PUTPROP FOO PROPSPEC FOO-PROP>",
                "<DEFINE FOO-PROP (L) (<> <EVAL <CHTYPE (TABLE <VOC \"FOO\" PREP>) FORM>>)>",
                "<OBJECT BAR (FOO FOO)>")
                .ImpliesAsync(
                    "<=? <GET <GETP ,BAR ,P?FOO> 0> ,W?FOO>");
        }

        [TestMethod]
        public async Task Routines_Created_By_PROPSPEC_Should_Work_Correctly()
        {
            await AssertGlobals(
                "<PUTPROP FOO PROPSPEC FOO-PROP>",
                "<DEFINE FOO-PROP (L) <ROUTINE PROP-ROUTINE () 123> (<> PROP-ROUTINE)>",
                "<OBJECT BAR (FOO FOO)>")
                .ImpliesAsync(
                    "<=? <APPLY <GETP ,BAR ,P?FOO>> 123>");
        }

        #endregion

        [TestMethod]
        public async Task Non_Constants_As_Property_Values_Should_Be_Rejected()
        {
            await AssertGlobals(
                "<GLOBAL FOO 123>",
                "<OBJECT BAR (BAZ FOO)>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task Non_Constants_In_Property_Initializers_Should_Be_Rejected()
        {
            await AssertGlobals(
                "<GLOBAL FOO 123>",
                "<OBJECT BAR (BAZ 4 5 FOO)>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task Nonexistent_Object_In_Direction_Property_Should_Be_Rejected()
        {
            await AssertGlobals(
                "<DIRECTIONS NORTH>",
                "<OBJECT FOO (NORTH TO BAR)>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task Direction_Synonyms_Should_Work_Identically()
        {
            await AssertGlobals(
                "<DIRECTIONS SOUTHWEST>",
                "<SYNONYM SOUTHWEST SW>",
                "<OBJECT FOO (SW TO FOO)>")
                .InV3()
                .ImpliesAsync(
                    "<=? ,P?SOUTHWEST ,P?SW>",
                    "<=? <GETB ,W?SW 5> ,P?SOUTHWEST>",
                    "<=? <GETB ,W?SOUTHWEST 5> ,P?SOUTHWEST>");
        }

        [TestMethod]
        public async Task Direction_Properties_Should_Not_Be_Merged_With_Words()
        {
            await AssertGlobals(
                "<DIRECTIONS NORTHNORTHEAST NORTHNORTHWEST>",
                "<OBJECT FOO (NORTHNORTHEAST TO FOO) (NORTHNORTHWEST TO BAR)>",
                "<OBJECT BAR>")
                .InV3()
                .ImpliesAsync(
                    "<=? ,W?NORTHNORTHEAST ,W?NORTHNORTHWEST>",
                    "<N=? ,P?NORTHNORTHEAST ,P?NORTHNORTHWEST>",
                    "<=? <GETP ,FOO ,P?NORTHNORTHEAST> ,FOO>",
                    "<=? <GETP ,FOO ,P?NORTHNORTHWEST> ,BAR>");
        }

        [TestMethod]
        public async Task ROOM_In_PROPDEF_Should_Be_One_Byte_When_ORDER_OBJECTS_Is_ROOMS_FIRST()
        {
            await AssertGlobals(
                "<ORDER-OBJECTS? ROOMS-FIRST>",
                "<DIRECTIONS NORTH>",
                "<PROPDEF DIRECTIONS <> (DIR TO R:ROOM = (UEXIT 1) (REXIT <ROOM .R>))>",
                "<OBJECT FOO (NORTH TO BAR)>",
                "<OBJECT BAR>")
                .InV5()
                .ImpliesAsync(
                    "<=? <PTSIZE <GETPT ,FOO ,P?NORTH>> 1>");
        }

        [TestMethod]
        public async Task Duplicate_Property_Definitions_Should_Not_Be_Allowed()
        {
            // user-defined property
            await AssertGlobals(
                "<OBJECT FOO (MYPROP 1) (MYPROP 2)>")
                .DoesNotCompileAsync();

            // standard pseudo-properties
            await AssertGlobals(
                "<OBJECT FOO (DESC \"foo\") (DESC \"bar\")>")
                .DoesNotCompileAsync();

            await AssertGlobals(
                "<OBJECT ROOM1>",
                "<OBJECT ROOM2>",
                "<OBJECT FOO (IN ROOM1) (LOC ROOM2)>")
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task IN_Pseudo_Property_Should_Not_Conflict_With_IN_String_NEXIT()
        {
            await AssertGlobals(
                "<DIRECTIONS IN>",
                "<OBJECT ROOMS>",
                "<OBJECT FOO (IN ROOMS) (IN \"You can't go in.\")>")
                .CompilesAsync();

            // even if IN isn't defined as a direction!
            await AssertGlobals(
                "<OBJECT ROOMS>",
                "<OBJECT FOO (IN ROOMS) (IN \"You can't go in.\")>")
                .CompilesAsync();
        }

        [TestMethod]
        public async Task Multiple_FLAGS_Definitions_Should_Combine()
        {
            await AssertGlobals(
                "<OBJECT FOO (FLAGS FOOBIT) (FLAGS BARBIT)>")
                .ImpliesAsync(
                    "<FSET? ,FOO ,FOOBIT>",
                    "<FSET? ,FOO ,BARBIT>");
        }

        [TestMethod]
        public async Task Mentioning_A_Routine_As_An_Object_Should_Not_Throw()
        {
            await AssertGlobals(
                    @"<ROOM WEST-SIDE-OF-FISSURE
                      (DESC ""West Side of Fissure"")>",
                    @"<ROUTINE WEST-SIDE-OF-FISSURE-F (RARG) <>>",
                    @"<OBJECT DIAMONDS (DESC ""diamonds"") (IN WEST-SIDE-OF-FISSURE-F)>")
                .WithoutWarnings()
                .DoesNotCompileAsync();
        }

        [TestMethod]
        public async Task DESC_Pseudo_Property_Should_Be_Stripped_Of_Newlines()
        {
            await AssertRoutine("",
                "<PRINTD ,FOO>")
                .WithGlobal("<OBJECT FOO (DESC \"first\nsecond\r\nthird\")>")
                .OutputsAsync("first second third");
        }

        [TestMethod]
        public async Task Unused_Flags_Should_Warn()
        {
            // only referenced in one object definition - warning
            await AssertGlobals("<OBJECT FOO (FLAGS MYBIT)>")
                .WithWarnings("ZIL0211")
                .CompilesAsync();

            // referenced in two object definitions - warning
            await AssertGlobals(
                "<OBJECT FOO (FLAGS MYBIT)>",
                "<OBJECT BAR (FLAGS MYBIT)>")
                .WithWarnings("ZIL0211")
                .CompilesAsync();

            // referenced in a routine - no warning
            await AssertRoutine("", "<FCLEAR ,FOO ,MYBIT>")
                .WithGlobal("<OBJECT FOO (FLAGS MYBIT)>")
                .WithoutWarnings()
                .CompilesAsync();

            // referenced in syntax - no warning
            await AssertGlobals(
                "<OBJECT FOO (FLAGS MYBIT MYBIT2)>",
                "<SYNTAX BLAH OBJECT (FIND MYBIT) WITH OBJECT (FIND MYBIT2) = V-BLAH>",
                "<ROUTINE V-BLAH () <>>")
                .WithoutWarnings()
                .CompilesAsync();
        }

        [TestMethod]
        public async Task Unused_Properties_Should_Warn()
        {
            // only referenced in one object definition - warning
            await AssertGlobals("<OBJECT FOO (MYPROP 123)>")
                .WithWarnings("ZIL0212")
                .CompilesAsync();

            // referenced in two object definitions - warning
            await AssertGlobals(
                "<OBJECT FOO (MYPROP 123)>",
                "<OBJECT BAR (MYPROP 456)>")
                .WithWarnings("ZIL0212")
                .CompilesAsync();

            // referenced in a routine - no warning
            await AssertRoutine("", "<GETP ,FOO ,P?MYPROP>")
                .WithGlobal("<OBJECT FOO (MYPROP 123)>")
                .WithoutWarnings()
                .CompilesAsync();
        }

        [TestMethod]
        public async Task Vocab_Properties_With_Apostrophes_Should_Warn()
        {
            await AssertGlobals("<OBJECT CATS-PAJAMAS (SYNONYM PAJAMAS) (ADJECTIVE CAT'S)>")
                .WithWarnings("MDL0429")
                .CompilesAsync();

            await AssertGlobals("<OBJECT FOO (SYNONYM WOULDN'T'VE) (ADJECTIVE 90'S)>")
                .WithWarnings("MDL0429")
                .DoesNotCompileAsync(); // because 90 can't be an adjective
        }
    }
}
